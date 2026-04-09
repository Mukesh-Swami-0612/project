using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ecom.Catalog.Infrastructure.Messaging.Consumers;

/// <summary>
/// Example RabbitMQ consumer that listens for inventory updates
/// and updates the ProductReadModel accordingly
/// </summary>
public class InventoryUpdatedConsumer : BackgroundService
{
    private readonly ILogger<InventoryUpdatedConsumer> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public InventoryUpdatedConsumer(
        ILogger<InventoryUpdatedConsumer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            InitializeRabbitMQ();

            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("RabbitMQ channel not available. Consumer will not start.");
                return;
            }

            // Declare queue
            _channel.QueueDeclare(
                queue: "catalog.inventory-updated",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: "catalog.inventory-updated",
                exchange: "inventory-events",
                routingKey: "inventory.updated");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogInformation("Received inventory update: {Message}", message);

                    // Parse message
                    var inventoryUpdate = JsonSerializer.Deserialize<InventoryUpdatedEvent>(message);
                    
                    if (inventoryUpdate != null)
                    {
                        // TODO: Update ProductReadModel with new stock information
                        // This would typically involve:
                        // 1. Get IServiceScope
                        // 2. Resolve IReadModelRepository
                        // 3. Update the read model
                        // 4. Save changes
                        
                        _logger.LogInformation(
                            "Processing inventory update for ProductId: {ProductId}, Stock: {Stock}",
                            inventoryUpdate.ProductId,
                            inventoryUpdate.Stock);
                    }

                    // Acknowledge message
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing inventory update message");
                    // Reject and requeue message
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(
                queue: "catalog.inventory-updated",
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("InventoryUpdatedConsumer started successfully");

            // Keep the consumer running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start InventoryUpdatedConsumer");
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
                UserName = _configuration["RabbitMq:Username"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _logger.LogInformation("RabbitMQ consumer connection established");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ for consumer");
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

// Example event structure
public class InventoryUpdatedEvent
{
    public int ProductId { get; set; }
    public int Stock { get; set; }
    public DateTime UpdatedAt { get; set; }
}
