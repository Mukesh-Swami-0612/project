using System.Text;
using System.Text.Json;
using Ecom.Catalog.Domain.Events;
using Ecom.Catalog.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;

namespace Ecom.Catalog.Infrastructure.Messaging;

public class CatalogEventPublisher
{
    private readonly ILogger<CatalogEventPublisher> _logger;
    private readonly IConfiguration _configuration;
    private readonly ResiliencePipeline<bool> _resiliencePolicy;
    private IConnection? _connection;
    private IModel? _channel;

    public CatalogEventPublisher(ILogger<CatalogEventPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _resiliencePolicy = ResiliencePolicies.CreateCombinedPolicy<bool>(logger);
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

    public async Task PublishProductCreatedAsync(ProductCreatedEvent @event)
    {
        _logger.LogInformation("Publishing ProductCreatedEvent for ProductId: {ProductId}", @event.ProductId);
        
        try
        {
            // 🔥 RESILIENCE: Apply retry + circuit breaker policy
            await _resiliencePolicy.ExecuteAsync(async cancellationToken =>
            {
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
                    _logger.LogWarning("RabbitMQ channel not available, attempting to reinitialize...");
                    InitializeRabbitMQ();
                    
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

                        _logger.LogInformation("ProductCreatedEvent published to RabbitMQ after reconnection");
                    }
                    else
                    {
                        throw new InvalidOperationException("RabbitMQ channel unavailable after reconnection attempt");
                    }
                }

                return true;
            }, CancellationToken.None);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(
                "EVENT_PUBLISH_CIRCUIT_OPEN | ProductId: {ProductId} | Circuit breaker is open, event not published",
                @event.ProductId);
            // Don't throw - log and continue (event will be lost, but system remains stable)
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "EVENT_PUBLISH_FAILED | ProductId: {ProductId} | Error: {Error}",
                @event.ProductId,
                ex.Message);
            // Don't throw - log and continue (graceful degradation)
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
