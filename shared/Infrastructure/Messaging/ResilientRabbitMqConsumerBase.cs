using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Ecom.Shared.Infrastructure.Messaging;

/// <summary>
/// 🔥 RESILIENT: Enhanced RabbitMQ consumer base with retry, DLQ, and idempotency
/// </summary>
public abstract class ResilientRabbitMqConsumerBase : BackgroundService
{
    protected IConnection? _connection;
    protected IModel? _channel;
    protected readonly IConfiguration _configuration;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    private readonly ResiliencePipeline _retryStrategy;
    private readonly ResiliencePipeline _circuitBreakerStrategy;
    private readonly HashSet<string> _processedMessages = new();
    private readonly object _idempotencyLock = new();
    
    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }
    protected virtual int MaxRetryAttempts => 3;
    protected virtual TimeSpan MessageTtl => TimeSpan.FromMinutes(30);
    protected virtual bool EnableIdempotency => true;

    protected ResilientRabbitMqConsumerBase(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;

        // 🔥 RESILIENCE: Retry strategy for message processing
        _retryStrategy = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("Message processing retry attempt {Attempt} for queue {Queue}: {Exception}", 
                        args.AttemptNumber, QueueName, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // 🔥 RESILIENCE: Circuit breaker for consumer protection
        _circuitBreakerStrategy = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromMinutes(1),
                OnOpened = args =>
                {
                    _logger.LogError("Consumer circuit breaker opened for queue {Queue}: {Exception}", 
                        QueueName, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Consumer circuit breaker closed for queue {Queue}", QueueName);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await InitializeRabbitMQAsync();
                
                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogWarning("RabbitMQ channel not available for {Queue}, retrying in 10 seconds", QueueName);
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                    continue;
                }

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    await HandleMessageAsync(ea);
                };

                _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
                
                _logger.LogInformation("Resilient consumer started for {Queue}", QueueName);
                
                // Keep running until cancellation
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer {Queue} stopping due to cancellation", QueueName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in consumer {Queue}, restarting in 30 seconds", QueueName);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var messageId = ea.BasicProperties?.MessageId ?? Guid.NewGuid().ToString();
        var retryCount = GetRetryCount(ea.BasicProperties);
        
        try
        {
            // 🔥 IDEMPOTENCY: Check if message already processed
            if (EnableIdempotency && IsMessageProcessed(messageId))
            {
                _logger.LogInformation("Duplicate message {MessageId} detected for queue {Queue}, acknowledging", 
                    messageId, QueueName);
                _channel?.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            _logger.LogInformation("Processing message {MessageId} (attempt {Retry}) on queue {Queue}", 
                messageId, retryCount + 1, QueueName);

            // 🔥 RESILIENCE: Apply circuit breaker and retry policies
            var success = await _circuitBreakerStrategy.ExecuteAsync(async cancellationToken =>
            {
                return await _retryStrategy.ExecuteAsync(async ct =>
                {
                    return await ProcessMessageSafelyAsync(message, messageId);
                }, cancellationToken);
            }, CancellationToken.None);

            if (success)
            {
                // 🔥 IDEMPOTENCY: Mark message as processed
                if (EnableIdempotency)
                {
                    MarkMessageAsProcessed(messageId);
                }
                
                _channel?.BasicAck(ea.DeliveryTag, false);
                _logger.LogInformation("Message {MessageId} processed successfully on queue {Queue}", 
                    messageId, QueueName);
            }
            else
            {
                await HandleFailedMessage(ea, messageId, retryCount);
            }
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("Circuit breaker open - rejecting message {MessageId} on queue {Queue}", 
                messageId, QueueName);
            _channel?.BasicNack(ea.DeliveryTag, false, true); // Requeue for later
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing message {MessageId} on queue {Queue}", 
                messageId, QueueName);
            await HandleFailedMessage(ea, messageId, retryCount);
        }
    }

    private async Task<bool> ProcessMessageSafelyAsync(string message, string messageId)
    {
        try
        {
            await ProcessMessageAsync(message, messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}: {Error}", messageId, ex.Message);
            return false;
        }
    }

    private async Task HandleFailedMessage(BasicDeliverEventArgs ea, string messageId, int retryCount)
    {
        if (retryCount >= MaxRetryAttempts)
        {
            // 🔥 DLQ: Send to Dead Letter Queue after max retries
            _logger.LogError("Message {MessageId} exceeded max retries ({MaxRetries}), sending to DLQ", 
                messageId, MaxRetryAttempts);
            
            await SendToDeadLetterQueue(ea, "Max retries exceeded");
            _channel?.BasicAck(ea.DeliveryTag, false); // Remove from main queue
        }
        else
        {
            // 🔥 RETRY: Increment retry count and requeue with delay
            _logger.LogWarning("Message {MessageId} failed, requeuing for retry {Retry}/{MaxRetries}", 
                messageId, retryCount + 1, MaxRetryAttempts);
            
            await RequeueWithDelay(ea, retryCount + 1);
            _channel?.BasicAck(ea.DeliveryTag, false); // Remove from main queue (will be requeued)
        }
    }

    private async Task SendToDeadLetterQueue(BasicDeliverEventArgs ea, string reason)
    {
        try
        {
            var dlqExchange = $"{GetExchangeName()}.dlx";
            var dlqRoutingKey = $"{RoutingKey}.failed";
            
            var properties = _channel?.CreateBasicProperties();
            if (properties != null)
            {
                properties.Persistent = true;
                properties.Headers = new Dictionary<string, object>(ea.BasicProperties?.Headers ?? new Dictionary<string, object>())
                {
                    ["x-death-reason"] = reason,
                    ["x-death-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["x-original-queue"] = QueueName,
                    ["x-original-routing-key"] = RoutingKey
                };
            }

            _channel?.BasicPublish(
                exchange: dlqExchange,
                routingKey: dlqRoutingKey,
                basicProperties: properties,
                body: ea.Body);

            _logger.LogInformation("Message sent to DLQ: {DLQExchange}/{DLQRoutingKey}", dlqExchange, dlqRoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to DLQ");
        }
    }

    private async Task RequeueWithDelay(BasicDeliverEventArgs ea, int retryCount)
    {
        try
        {
            var delayExchange = $"{GetExchangeName()}.delay";
            var delayQueue = $"{QueueName}.delay.{retryCount}";
            var delayMs = (int)Math.Pow(2, retryCount) * 1000; // Exponential backoff
            
            // Declare delay queue with TTL
            var args = new Dictionary<string, object>
            {
                ["x-message-ttl"] = delayMs,
                ["x-dead-letter-exchange"] = GetExchangeName(),
                ["x-dead-letter-routing-key"] = RoutingKey
            };
            
            _channel?.QueueDeclare(delayQueue, durable: true, exclusive: false, autoDelete: false, arguments: args);
            
            var properties = _channel?.CreateBasicProperties();
            if (properties != null)
            {
                properties.Persistent = true;
                properties.Headers = new Dictionary<string, object>(ea.BasicProperties?.Headers ?? new Dictionary<string, object>())
                {
                    ["x-retry-count"] = retryCount,
                    ["x-retry-timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }

            _channel?.BasicPublish(
                exchange: "",
                routingKey: delayQueue,
                basicProperties: properties,
                body: ea.Body);

            _logger.LogInformation("Message requeued with {DelayMs}ms delay to {DelayQueue}", delayMs, delayQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to requeue message with delay");
        }
    }

    private async Task InitializeRabbitMQAsync()
    {
        try
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
                UserName = _configuration["RabbitMq:Username"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(60)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            await DeclareInfrastructureAsync();
            
            _logger.LogInformation("RabbitMQ initialized for resilient consumer {Queue}", QueueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for {Queue}", QueueName);
            throw;
        }
    }

    private async Task DeclareInfrastructureAsync()
    {
        var exchangeName = GetExchangeName();
        
        // 🔥 RESILIENCE: Declare main exchange
        _channel?.ExchangeDeclare(exchangeName, ExchangeType.Topic, durable: true);
        
        // 🔥 RESILIENCE: Declare Dead Letter Exchange
        var dlxName = $"{exchangeName}.dlx";
        _channel?.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);
        
        // 🔥 RESILIENCE: Declare delay exchange for retry mechanism
        var delayExchange = $"{exchangeName}.delay";
        _channel?.ExchangeDeclare(delayExchange, ExchangeType.Direct, durable: true);

        // 🔥 RESILIENCE: Declare main queue with DLX configuration
        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlxName,
            ["x-message-ttl"] = (int)MessageTtl.TotalMilliseconds
        };
        
        _channel?.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel?.QueueBind(QueueName, exchangeName, RoutingKey);

        // 🔥 RESILIENCE: Declare Dead Letter Queue
        var dlqName = $"{QueueName}.dlq";
        _channel?.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel?.QueueBind(dlqName, dlxName, $"{RoutingKey}.failed");

        _logger.LogInformation("RabbitMQ infrastructure declared for {Queue} with DLX {DLX}", QueueName, dlxName);
    }

    private string GetExchangeName()
    {
        return _configuration["RabbitMq:Exchange"] ?? "ecom.domain.events";
    }

    private int GetRetryCount(IBasicProperties? properties)
    {
        if (properties?.Headers?.TryGetValue("x-retry-count", out var retryObj) == true)
        {
            return retryObj is int retry ? retry : 0;
        }
        return 0;
    }

    private bool IsMessageProcessed(string messageId)
    {
        lock (_idempotencyLock)
        {
            return _processedMessages.Contains(messageId);
        }
    }

    private void MarkMessageAsProcessed(string messageId)
    {
        lock (_idempotencyLock)
        {
            _processedMessages.Add(messageId);
            
            // 🔥 MEMORY: Prevent memory leak by limiting processed message cache
            if (_processedMessages.Count > 10000)
            {
                var toRemove = _processedMessages.Take(5000).ToList();
                foreach (var id in toRemove)
                {
                    _processedMessages.Remove(id);
                }
                _logger.LogInformation("Cleaned up processed message cache for {Queue}", QueueName);
            }
        }
    }

    protected abstract Task ProcessMessageAsync(string message, string messageId);

    public override void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ resources for {Queue}", QueueName);
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}