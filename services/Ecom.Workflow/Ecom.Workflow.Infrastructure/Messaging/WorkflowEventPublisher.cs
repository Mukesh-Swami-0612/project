using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Ecom.Workflow.Infrastructure.Messaging;

public class WorkflowEventPublisher
{
    private readonly ILogger<WorkflowEventPublisher> _logger;
    private readonly IConfiguration _config;

    public WorkflowEventPublisher(ILogger<WorkflowEventPublisher> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task PublishAsync(string eventType, string payload)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
                UserName = _config["RabbitMq:Username"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? "guest"
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            const string workflowExchange = "workflow-events";

            // Declare exchange
            channel.ExchangeDeclare(
                exchange: workflowExchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            // Create message
            var message = new
            {
                EventType = eventType,
                Payload = JsonSerializer.Deserialize<object>(payload),
                Timestamp = DateTime.UtcNow,
                Source = "workflow-service"
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            // Publish message
            channel.BasicPublish(
                exchange: workflowExchange,
                routingKey: eventType,
                basicProperties: null,
                body: body
            );

            _logger.LogInformation("✅ Published event to RabbitMQ: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to publish event: {EventType}", eventType);
            throw;
        }

        return Task.CompletedTask;
    }
}
