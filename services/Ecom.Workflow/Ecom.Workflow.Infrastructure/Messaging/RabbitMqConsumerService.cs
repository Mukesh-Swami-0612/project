using System.Text;
using System.Text.Json;
using Ecom.Workflow.Application.Events;
using Ecom.Workflow.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Ecom.Workflow.Infrastructure.Messaging;

/// <summary>
/// Background service that subscribes to RabbitMQ events from Catalog service
/// Handles connection management, retry, and message routing to consumers
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ProductCreatedConsumer _productCreatedConsumer;
    private readonly ProductValidatedConsumer _productValidatedConsumer;
    private readonly ProductApprovedConsumer _productApprovedConsumer;
    private readonly ProductPublishedConsumer _productPublishedConsumer;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly object _shutdownLock = new();
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMqConsumerService(
        IConfiguration config,
        ProductCreatedConsumer productCreatedConsumer,
        ProductValidatedConsumer productValidatedConsumer,
        ProductApprovedConsumer productApprovedConsumer,
        ProductPublishedConsumer productPublishedConsumer,
        ILogger<RabbitMqConsumerService> logger)
    {
        _config = config;
        _productCreatedConsumer = productCreatedConsumer;
        _productValidatedConsumer = productValidatedConsumer;
        _productApprovedConsumer = productApprovedConsumer;
        _productPublishedConsumer = productPublishedConsumer;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service starting...");
        InitializeRabbitMQ();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service is running");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown path. StopAsync performs the RabbitMQ cleanup.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Consumer Service is stopping");
        CloseRabbitMqResources();
        await base.StopAsync(cancellationToken);
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
                UserName = _config["RabbitMq:Username"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange (should match Catalog's exchange)
            _channel.ExchangeDeclare(
                exchange: "catalog-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue for workflow service
            var queueName = "workflow-service-queue";
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange with routing keys
            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.created");

            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.validated");

            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.approved");

            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.published");

            _logger.LogInformation(
                "RabbitMQ queue bound: {QueueName} -> catalog-events (product.created, product.validated, product.approved, product.published)",
                queueName);

            // Setup consumer
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                await HandleMessageAsync(ea);
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("RabbitMQ Consumer Service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ");
            throw;
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var messageId = ea.BasicProperties.MessageId;

        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogDebug(
                "Received message | RoutingKey: {RoutingKey} | MessageId: {MessageId}",
                routingKey,
                messageId);

            // Route to appropriate consumer based on routing key
            switch (routingKey)
            {
                case "product.created":
                    var productCreatedEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(message);
                    if (productCreatedEvent != null)
                    {
                        await _productCreatedConsumer.HandleAsync(productCreatedEvent);
                    }
                    break;

                case "product.validated":
                    var productValidatedEvent = JsonSerializer.Deserialize<ProductValidatedEvent>(message);
                    if (productValidatedEvent != null)
                    {
                        await _productValidatedConsumer.HandleAsync(productValidatedEvent);
                    }
                    break;

                case "product.approved":
                    var productApprovedEvent = JsonSerializer.Deserialize<ProductApprovedEvent>(message);
                    if (productApprovedEvent != null)
                    {
                        await _productApprovedConsumer.HandleAsync(productApprovedEvent);
                    }
                    break;

                case "product.published":
                    var productPublishedEvent = JsonSerializer.Deserialize<ProductPublishedEvent>(message);
                    if (productPublishedEvent != null)
                    {
                        await _productPublishedConsumer.HandleAsync(productPublishedEvent);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown routing key: {RoutingKey}", routingKey);
                    break;
            }

            // Acknowledge message after successful processing
            _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

            _logger.LogDebug(
                "Message processed successfully | RoutingKey: {RoutingKey} | MessageId: {MessageId}",
                routingKey,
                messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process message | RoutingKey: {RoutingKey} | MessageId: {MessageId} | Error: {Error}",
                routingKey,
                messageId,
                ex.Message);

            // Negative acknowledge - requeue for retry
            _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        CloseRabbitMqResources();
        base.Dispose();
    }

    private void CloseRabbitMqResources()
    {
        lock (_shutdownLock)
        {
            if (_disposed)
            {
                return;
            }

            SafeClose(_channel, "RabbitMQ channel");
            SafeClose(_connection, "RabbitMQ connection");

            SafeDispose(_channel, "RabbitMQ channel");
            SafeDispose(_connection, "RabbitMQ connection");
            _channel = null;
            _connection = null;
            _disposed = true;
        }
    }

    private void SafeDispose(IDisposable? disposable, string resourceName)
    {
        if (disposable == null)
        {
            return;
        }

        try
        {
            disposable.Dispose();
        }
        catch (AlreadyClosedException)
        {
            _logger.LogDebug("{ResourceName} was already closed before dispose", resourceName);
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            _logger.LogDebug("{ResourceName} channel was already closed before dispose", resourceName);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("{ResourceName} was already disposed", resourceName);
        }
    }

    private void SafeClose(IModel? channel, string resourceName)
    {
        if (channel == null)
        {
            return;
        }

        try
        {
            if (channel.IsOpen)
            {
                channel.Close();
            }
        }
        catch (AlreadyClosedException)
        {
            _logger.LogDebug("{ResourceName} was already closed", resourceName);
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            _logger.LogDebug("{ResourceName} channel was already closed", resourceName);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("{ResourceName} was already disposed", resourceName);
        }
    }

    private void SafeClose(IConnection? connection, string resourceName)
    {
        if (connection == null)
        {
            return;
        }

        try
        {
            if (connection.IsOpen)
            {
                connection.Close();
            }
        }
        catch (AlreadyClosedException)
        {
            _logger.LogDebug("{ResourceName} was already closed", resourceName);
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            _logger.LogDebug("{ResourceName} channel was already closed", resourceName);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("{ResourceName} was already disposed", resourceName);
        }
    }
}
