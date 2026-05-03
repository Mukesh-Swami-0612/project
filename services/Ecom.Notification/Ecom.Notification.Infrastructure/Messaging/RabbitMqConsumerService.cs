using System.Text;
using System.Text.Json;
using Ecom.Notification.Domain.Events;
using Ecom.Notification.Infrastructure.Messaging.Consumers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ecom.Notification.Infrastructure.Messaging;

/// <summary>
/// Background service that subscribes to RabbitMQ events
/// Listens to product and workflow events to trigger notifications
/// </summary>
public class RabbitMqConsumerService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ProductApprovedConsumer _productApprovedConsumer;
    private readonly ProductRejectedConsumer _productRejectedConsumer;
    private readonly ProductPublishedConsumer _productPublishedConsumer;
    private readonly WorkflowFailedConsumer _workflowFailedConsumer;
    private readonly UserRegisteredConsumer _userRegisteredConsumer;
    private readonly UserLoginSuccessConsumer _userLoginSuccessConsumer;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqConsumerService(
        IConfiguration config,
        ProductApprovedConsumer productApprovedConsumer,
        ProductRejectedConsumer productRejectedConsumer,
        ProductPublishedConsumer productPublishedConsumer,
        WorkflowFailedConsumer workflowFailedConsumer,
        UserRegisteredConsumer userRegisteredConsumer,
        UserLoginSuccessConsumer userLoginSuccessConsumer,
        ILogger<RabbitMqConsumerService> logger)
    {
        _config = config;
        _productApprovedConsumer = productApprovedConsumer;
        _productRejectedConsumer = productRejectedConsumer;
        _productPublishedConsumer = productPublishedConsumer;
        _workflowFailedConsumer = workflowFailedConsumer;
        _userRegisteredConsumer = userRegisteredConsumer;
        _userLoginSuccessConsumer = userLoginSuccessConsumer;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("NOTIFICATION_RABBITMQ_CONSUMER_STARTING");
        InitializeRabbitMQ();
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NOTIFICATION_RABBITMQ_CONSUMER_RUNNING");

        stoppingToken.Register(() =>
        {
            _logger.LogInformation("NOTIFICATION_RABBITMQ_CONSUMER_STOPPING");
            _channel?.Close();
            _connection?.Close();
        });

        await Task.CompletedTask;
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

            // Declare catalog-events exchange
            _channel.ExchangeDeclare(
                exchange: "catalog-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare workflow-events exchange
            _channel.ExchangeDeclare(
                exchange: "workflow-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Backward compatibility for existing publishers using legacy workflow exchange name
            _channel.ExchangeDeclare(
                exchange: "ecom.workflow.events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare ecom-events exchange (unified exchange for auth events)
            _channel.ExchangeDeclare(
                exchange: "ecom-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue for notification service
            var queueName = "notification-service-queue";
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind to catalog events
            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.approved");

            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.rejected");

            _channel.QueueBind(
                queue: queueName,
                exchange: "catalog-events",
                routingKey: "product.published");

            // Bind to workflow events
            _channel.QueueBind(
                queue: queueName,
                exchange: "workflow-events",
                routingKey: "workflow.failed");

            _channel.QueueBind(
                queue: queueName,
                exchange: "ecom.workflow.events",
                routingKey: "workflow.failed");

            // Bind to auth events (from ecom-events exchange)
            _channel.QueueBind(
                queue: queueName,
                exchange: "ecom-events",
                routingKey: "user.registered");

            _channel.QueueBind(
                queue: queueName,
                exchange: "ecom-events",
                routingKey: "user.login.success");

            _logger.LogInformation(
                "NOTIFICATION_RABBITMQ_QUEUE_BOUND | Queue: {QueueName} | Events: product.*, workflow.failed, user.registered, user.login.success",
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

            _logger.LogInformation("NOTIFICATION_RABBITMQ_CONSUMER_INITIALIZED");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NOTIFICATION_RABBITMQ_INIT_FAILED | Error: {Error}", ex.Message);
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
                "NOTIFICATION_MESSAGE_RECEIVED | RoutingKey: {RoutingKey} | MessageId: {MessageId}",
                routingKey,
                messageId);

            // Route to appropriate consumer
            switch (routingKey)
            {
                case "product.approved":
                    var approvedEvent = JsonSerializer.Deserialize<ProductApprovedEvent>(message);
                    if (approvedEvent != null)
                    {
                        await _productApprovedConsumer.HandleAsync(approvedEvent);
                    }
                    break;

                case "product.rejected":
                    var rejectedEvent = JsonSerializer.Deserialize<ProductRejectedEvent>(message);
                    if (rejectedEvent != null)
                    {
                        await _productRejectedConsumer.HandleAsync(rejectedEvent);
                    }
                    break;

                case "product.published":
                    var publishedEvent = JsonSerializer.Deserialize<ProductPublishedEvent>(message);
                    if (publishedEvent != null)
                    {
                        await _productPublishedConsumer.HandleAsync(publishedEvent);
                    }
                    break;

                case "workflow.failed":
                    var failedEvent = JsonSerializer.Deserialize<WorkflowFailedEvent>(message);
                    if (failedEvent != null)
                    {
                        await _workflowFailedConsumer.HandleAsync(failedEvent);
                    }
                    break;

                case "user.registered":
                    var registeredEvent = JsonSerializer.Deserialize<UserRegisteredEvent>(message);
                    if (registeredEvent != null)
                    {
                        await _userRegisteredConsumer.HandleAsync(registeredEvent);
                    }
                    break;

                case "user.login.success":
                    var loginEvent = JsonSerializer.Deserialize<UserLoginSuccessEvent>(message);
                    if (loginEvent != null)
                    {
                        await _userLoginSuccessConsumer.HandleAsync(loginEvent);
                    }
                    break;

                default:
                    _logger.LogWarning("NOTIFICATION_UNKNOWN_ROUTING_KEY | RoutingKey: {RoutingKey}", routingKey);
                    break;
            }

            // Acknowledge message
            _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

            _logger.LogDebug(
                "NOTIFICATION_MESSAGE_PROCESSED | RoutingKey: {RoutingKey} | MessageId: {MessageId}",
                routingKey,
                messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "NOTIFICATION_MESSAGE_PROCESSING_FAILED | RoutingKey: {RoutingKey} | MessageId: {MessageId} | Error: {Error}",
                routingKey,
                messageId,
                ex.Message);

            // Negative acknowledge - requeue for retry
            _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
