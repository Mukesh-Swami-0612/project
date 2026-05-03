using Ecom.Shared.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;

namespace Ecom.Shared.Infrastructure.Messaging;

public class ResilientRabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly ILogger<ResilientRabbitMqPublisher> _logger;
    private readonly string _exchangeName;
    private readonly ConnectionFactory _connectionFactory;
    private readonly ResiliencePipeline _retryStrategy;
    private readonly ResiliencePipeline _circuitBreakerStrategy;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly object _lock = new();

    public ResilientRabbitMqPublisher(
        string host, 
        int port, 
        string username, 
        string password, 
        string exchangeName,
        ILogger<ResilientRabbitMqPublisher> logger)
    {
        _logger = logger;
        _exchangeName = exchangeName;
        
        _connectionFactory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            DispatchConsumersAsync = true
        };

        // 🔥 RESILIENCE: Retry strategy with exponential backoff
        _retryStrategy = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
                    .Handle<AlreadyClosedException>()
                    .Handle<OperationInterruptedException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("RabbitMQ publish retry attempt {Attempt} for {Exception}", 
                        args.AttemptNumber, args.Outcome.Exception?.GetType().Name);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // 🔥 RESILIENCE: Circuit breaker to prevent cascading failures
        _circuitBreakerStrategy = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>()
                    .Handle<AlreadyClosedException>()
                    .Handle<OperationInterruptedException>(),
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogError("RabbitMQ circuit breaker opened due to {Exception}", 
                        args.Outcome.Exception?.GetType().Name);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("RabbitMQ circuit breaker closed - connection restored");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation("RabbitMQ circuit breaker half-opened - testing connection");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        EnsureConnection();
        DeclareExchangeAndQueues();
    }

    private void EnsureConnection()
    {
        lock (_lock)
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            try
            {
                _connection?.Close();
                _channel?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing existing RabbitMQ connection");
            }

            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _logger.LogInformation("RabbitMQ connection established to {Host}:{Port}", 
                _connectionFactory.HostName, _connectionFactory.Port);
        }
    }

    private void DeclareExchangeAndQueues()
    {
        if (_channel == null) return;

        // 🔥 RESILIENCE: Declare main exchange
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Topic, durable: true);

        // 🔥 RESILIENCE: Declare Dead Letter Exchange (DLX)
        var dlxName = $"{_exchangeName}.dlx";
        _channel.ExchangeDeclare(dlxName, ExchangeType.Direct, durable: true);

        // 🔥 RESILIENCE: Declare Dead Letter Queue (DLQ)
        var dlqName = $"{_exchangeName}.dlq";
        _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(dlqName, dlxName, "");

        _logger.LogInformation("RabbitMQ exchange '{Exchange}' and DLQ '{DLQ}' declared", 
            _exchangeName, dlqName);
    }

    public async Task PublishAsync<T>(T @event, string routingKey) where T : class
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));
        if (string.IsNullOrEmpty(routingKey)) throw new ArgumentException("Routing key cannot be empty", nameof(routingKey));

        try
        {
            // 🔥 RESILIENCE: Apply circuit breaker and retry policies
            await _circuitBreakerStrategy.ExecuteAsync(async cancellationToken =>
            {
                await _retryStrategy.ExecuteAsync(async ct =>
                {
                    PublishMessage(@event, routingKey);
                }, cancellationToken);
            }, CancellationToken.None);

            _logger.LogDebug("Event {EventType} published successfully with routing key {RoutingKey}", 
                typeof(T).Name, routingKey);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogError("RabbitMQ circuit breaker is open - event {EventType} not published", typeof(T).Name);
            throw new InvalidOperationException("Message broker is currently unavailable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} with routing key {RoutingKey}", 
                typeof(T).Name, routingKey);
            throw;
        }
    }

    private void PublishMessage<T>(T @event, string routingKey) where T : class
    {
        EnsureConnection();

        if (_channel == null || !_channel.IsOpen)
            throw new InvalidOperationException("RabbitMQ channel is not available");

        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var body = Encoding.UTF8.GetBytes(json);

        // 🔥 RESILIENCE: Message properties with retry and DLQ support
        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // Survive broker restart
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        properties.ContentType = "application/json";
        properties.Type = typeof(T).Name;
        
        // 🔥 RESILIENCE: Set message TTL and DLX for failed messages
        properties.Headers = new Dictionary<string, object>
        {
            ["x-message-ttl"] = 300000, // 5 minutes TTL
            ["x-dead-letter-exchange"] = $"{_exchangeName}.dlx",
            ["x-retry-count"] = 0
        };

        _channel.BasicPublish(
            exchange: _exchangeName,
            routingKey: routingKey,
            basicProperties: properties,
            body: body);
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _connection?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ connection");
        }
        finally
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}