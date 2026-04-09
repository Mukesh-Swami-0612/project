using System.Text;
using System.Text.Json;
using Ecom.Shared.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Ecom.Shared.Infrastructure.Messaging;

public class RabbitMqPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private readonly string _exchangeName;

    public RabbitMqPublisher(string hostName, string exchangeName, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;
        _exchangeName = exchangeName;

        var factory = new ConnectionFactory
        {
            HostName = hostName,
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ Publisher connected to {HostName}, Exchange: {Exchange}", hostName, exchangeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ at {HostName}", hostName);
            throw;
        }
    }

    public Task PublishAsync<T>(T @event, string routingKey = "") where T : class
    {
        try
        {
            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: string.IsNullOrEmpty(routingKey) ? typeof(T).Name : routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body);

            _logger.LogInformation(
                "Published event {EventType} to exchange {Exchange} with routing key {RoutingKey}",
                typeof(T).Name, _exchangeName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
            throw;
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
    }
}
