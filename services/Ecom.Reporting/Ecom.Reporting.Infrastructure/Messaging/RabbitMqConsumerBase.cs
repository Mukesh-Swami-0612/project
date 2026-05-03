using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ecom.Reporting.Infrastructure.Messaging;

public abstract class RabbitMqConsumerBase : BackgroundService
{
    protected IConnection? _connection;
    protected IModel? _channel;
    protected readonly IConfiguration _configuration;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    
    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    protected RabbitMqConsumerBase(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            InitializeRabbitMQ();
            
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("RabbitMQ channel not available for {Queue}", QueueName);
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var retryCount = 0;
                if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
                {
                    retryCount = Convert.ToInt32(ea.BasicProperties.Headers["x-retry-count"]);
                }

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogInformation(
                        "Received message on {Queue} (Retry: {RetryCount}): {Message}", 
                        QueueName, retryCount, message);
                    
                    await ProcessMessageAsync(message);
                    
                    _channel.BasicAck(ea.DeliveryTag, false);
                    
                    _logger.LogInformation(
                        "Successfully processed message on {Queue} (Retry: {RetryCount})", 
                        QueueName, retryCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex, 
                        "Error processing message on {Queue} (Retry: {RetryCount}/{MaxRetries})", 
                        QueueName, retryCount, 3);

                    // 🔥 PRODUCTION: Retry logic with max attempts
                    if (retryCount < 3)
                    {
                        // Increment retry count and requeue
                        var properties = _channel.CreateBasicProperties();
                        properties.Headers = new Dictionary<string, object>
                        {
                            ["x-retry-count"] = retryCount + 1,
                            ["x-first-death-reason"] = ex.Message,
                            ["x-first-death-queue"] = QueueName,
                            ["x-first-death-time"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        };
                        properties.Persistent = true;

                        _channel.BasicPublish(
                            exchange: "",
                            routingKey: QueueName,
                            basicProperties: properties,
                            body: ea.Body);

                        _channel.BasicAck(ea.DeliveryTag, false);
                        
                        _logger.LogWarning(
                            "Message requeued for retry {RetryCount}/3 on {Queue}", 
                            retryCount + 1, QueueName);
                    }
                    else
                    {
                        // 🔥 PRODUCTION: Max retries exceeded, send to DLQ
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                        
                        _logger.LogError(
                            "MESSAGE_SENT_TO_DLQ | Queue: {Queue} | Reason: Max retries exceeded | Error: {Error}",
                            QueueName, ex.Message);
                    }
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            
            _logger.LogInformation("Consumer started for {Queue}", QueueName);
            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in consumer {Queue}", QueueName);
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var exchanges = (_configuration["RabbitMq:Exchanges"]
                ?? "catalog-events,auth-events,workflow-events,ecom.workflow.events,ecom.domain.events")
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
                UserName = _configuration["RabbitMq:Username"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            foreach (var exchange in exchanges)
            {
                _channel.ExchangeDeclare(
                    exchange: exchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);
            }

            // 🔥 PRODUCTION: Declare Dead Letter Exchange (DLX)
            var dlxName = $"{QueueName}.dlx";
            _channel.ExchangeDeclare(
                exchange: dlxName,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // 🔥 PRODUCTION: Declare Dead Letter Queue (DLQ)
            var dlqName = $"{QueueName}.dlq";
            _channel.QueueDeclare(
                queue: dlqName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.QueueBind(
                queue: dlqName,
                exchange: dlxName,
                routingKey: "");

            // 🔥 PRODUCTION: Declare main queue with DLX configuration
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = dlxName,
                ["x-message-ttl"] = 300000, // 5 minutes TTL
                ["x-max-length"] = 10000 // Max 10k messages
            };

            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs);

            foreach (var exchange in exchanges)
            {
                _channel.QueueBind(
                    queue: QueueName,
                    exchange: exchange,
                    routingKey: RoutingKey);
            }

            _logger.LogInformation(
                "RabbitMQ initialized for {Queue} with DLQ {DLQ} and routing key {RoutingKey} on exchanges {Exchanges}",
                QueueName, dlqName, RoutingKey, string.Join(", ", exchanges));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for {Queue}", QueueName);
        }
    }

    protected abstract Task ProcessMessageAsync(string message);

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
