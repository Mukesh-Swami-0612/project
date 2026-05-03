using System.Text;
using System.Text.Json;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Events;
using Ecom.Catalog.Infrastructure.EventHandlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Ecom.Catalog.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes outbox messages and publishes them to RabbitMQ
/// Runs every 5 seconds to check for pending messages
/// Guarantees at-least-once delivery
/// </summary>
public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);
    private IConnection? _connection;
    private IModel? _channel;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox Processor Service starting...");
        InitializeRabbitMQ();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        // Get pending messages
        var pendingMessages = await outboxRepository.GetPendingMessagesAsync(batchSize: 20);

        foreach (var message in pendingMessages)
        {
            try
            {
                // Mark as processing to prevent duplicate processing
                await outboxRepository.MarkAsProcessingAsync(message.Id);

                // 🔥 CQRS: Invoke event handlers to update read model
                await InvokeEventHandlersAsync(scope, message);

                // Publish to RabbitMQ
                await PublishToRabbitMQAsync(message);

                // Mark as processed
                await outboxRepository.MarkAsProcessedAsync(message.Id);

                _logger.LogInformation(
                    "Successfully processed outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to process outbox message {MessageId} of type {EventType}",
                    message.Id,
                    message.EventType);

                // Mark as failed with retry count
                await outboxRepository.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    message.RetryCount + 1);
            }
        }

        // Process retryable failed messages
        var retryableMessages = await outboxRepository.GetRetryableMessagesAsync(maxRetries: 3);
        foreach (var message in retryableMessages)
        {
            try
            {
                await outboxRepository.MarkAsProcessingAsync(message.Id);
                await InvokeEventHandlersAsync(scope, message);
                await PublishToRabbitMQAsync(message);
                await outboxRepository.MarkAsProcessedAsync(message.Id);

                _logger.LogInformation(
                    "Successfully retried outbox message {MessageId} (retry {RetryCount})",
                    message.Id,
                    message.RetryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retry outbox message {MessageId} (retry {RetryCount})",
                    message.Id,
                    message.RetryCount);

                await outboxRepository.MarkAsFailedAsync(
                    message.Id,
                    ex.Message,
                    message.RetryCount + 1);
            }
        }
    }

    private Task PublishToRabbitMQAsync(OutboxMessage message)
    {
        if (_channel == null || !_channel.IsOpen)
        {
            _logger.LogWarning("RabbitMQ channel not available, reinitializing...");
            InitializeRabbitMQ();
        }

        if (_channel != null && _channel.IsOpen)
        {
            var body = Encoding.UTF8.GetBytes(message.Payload);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = message.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(
                ((DateTimeOffset)message.OccurredOn).ToUnixTimeSeconds());

            // Determine routing key based on event type
            var routingKey = GetRoutingKey(message.EventType);

            _channel.BasicPublish(
                exchange: "catalog-events",
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug(
                "Published message {MessageId} to RabbitMQ with routing key {RoutingKey}",
                message.Id,
                routingKey);
        }
        else
        {
            throw new InvalidOperationException("RabbitMQ channel is not available");
        }

        return Task.CompletedTask;
    }

    private string GetRoutingKey(string eventType)
    {
        return eventType switch
        {
            "ProductCreatedEvent" => "product.created",
            "ProductUpdatedEvent" => "product.updated",
            "ProductValidatedEvent" => "product.validated",
            "ProductStatusChangedEvent" => "product.status.changed",
            "ProductApprovedEvent" => "product.approved",
            "ProductRejectedEvent" => "product.rejected",
            "ProductPublishedEvent" => "product.published",
            _ => "product.unknown"
        };
    }

    /// <summary>
    /// Invoke event handlers to update read model
    /// Ensures eventual consistency between write and read models
    /// </summary>
    private async Task InvokeEventHandlersAsync(IServiceScope scope, OutboxMessage message)
    {
        try
        {
            switch (message.EventType)
            {
                case "ProductCreatedEvent":
                    var createdEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(message.Payload);
                    if (createdEvent != null)
                    {
                        var createdHandler = scope.ServiceProvider.GetRequiredService<ProductCreatedEventHandler>();
                        await createdHandler.HandleAsync(createdEvent);
                    }
                    break;

                case "ProductUpdatedEvent":
                    var updatedEvent = JsonSerializer.Deserialize<ProductUpdatedEvent>(message.Payload);
                    if (updatedEvent != null)
                    {
                        var updatedHandler = scope.ServiceProvider.GetRequiredService<ProductUpdatedEventHandler>();
                        await updatedHandler.HandleAsync(updatedEvent);
                    }
                    break;

                // Other events don't need read model updates
                default:
                    _logger.LogDebug("No event handler for {EventType}", message.EventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invoke event handler for {EventType}", message.EventType);
            throw;
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost",
                Port = int.Parse(Environment.GetEnvironmentVariable("RabbitMq__Port") ?? "5672"),
                UserName = Environment.GetEnvironmentVariable("RabbitMq__Username") ?? "guest",
                Password = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: "catalog-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Outbox Processor Service stopping...");
        
        _channel?.Close();
        _connection?.Close();
        
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
