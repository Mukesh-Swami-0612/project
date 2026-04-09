using System.Text;
using System.Text.Json;
using Ecom.Catalog.Domain.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Ecom.Catalog.Infrastructure.Messaging;

public class CatalogEventPublisher
{
    private readonly ILogger<CatalogEventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private IConnection? _connection;
    private IModel? _channel;

    public CatalogEventPublisher(ILogger<CatalogEventPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        InitializeRabbitMQ();
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
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Events will be logged only.");
        }
    }

    public Task PublishProductCreatedAsync(ProductCreatedEvent @event)
    {
        _logger.LogInformation("Publishing ProductCreatedEvent for ProductId: {ProductId}", @event.ProductId);
        
        if (_channel != null && _channel.IsOpen)
        {
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            _channel.BasicPublish(
                exchange: "catalog-events",
                routingKey: "product.created",
                basicProperties: properties,
                body: body);

            _logger.LogInformation("ProductCreatedEvent published to RabbitMQ");
        }
        else
        {
            _logger.LogWarning("RabbitMQ channel not available. Event logged only.");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
