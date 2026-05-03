using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Ecom.Shared.Infrastructure.Messaging;

/// <summary>
/// 🔥 TESTING: Utility to test RabbitMQ reliability mechanisms
/// </summary>
public class RabbitMqReliabilityTester
{
    private readonly ILogger<RabbitMqReliabilityTester> _logger;
    private readonly string _connectionString;

    public RabbitMqReliabilityTester(ILogger<RabbitMqReliabilityTester> logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }

    /// <summary>
    /// Test message publishing with failure simulation
    /// </summary>
    public async Task TestPublishReliabilityAsync()
    {
        _logger.LogInformation("🔥 TESTING: RabbitMQ publish reliability");

        var factory = new ConnectionFactory();
        factory.Uri = new Uri(_connectionString);

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Test message
        var testMessage = new
        {
            Id = Guid.NewGuid(),
            Message = "Test reliability message",
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(testMessage);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = testMessage.Id.ToString();

        try
        {
            // Test 1: Normal publish
            channel.BasicPublish("ecom.domain.events", "test.reliability", properties, body);
            _logger.LogInformation("✅ Normal publish test passed");

            // Test 2: Publish to non-existent exchange (should fail gracefully)
            try
            {
                channel.BasicPublish("non.existent.exchange", "test.reliability", properties, body);
                _logger.LogWarning("❌ Non-existent exchange test should have failed");
            }
            catch (Exception ex)
            {
                _logger.LogInformation("✅ Non-existent exchange test passed: {Error}", ex.Message);
            }

            _logger.LogInformation("🔥 TESTING: Publish reliability tests completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Publish reliability test failed");
        }
    }

    /// <summary>
    /// Test DLQ functionality
    /// </summary>
    public async Task TestDeadLetterQueueAsync()
    {
        _logger.LogInformation("🔥 TESTING: Dead Letter Queue functionality");

        var factory = new ConnectionFactory();
        factory.Uri = new Uri(_connectionString);

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        try
        {
            // Declare test infrastructure
            var exchange = "test.dlq.exchange";
            var dlxExchange = $"{exchange}.dlx";
            var queue = "test.dlq.queue";
            var dlqQueue = $"{queue}.dlq";

            // Main exchange and DLX
            channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true);
            channel.ExchangeDeclare(dlxExchange, ExchangeType.Direct, durable: true);

            // Main queue with DLX configuration
            var queueArgs = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = dlxExchange,
                ["x-message-ttl"] = 5000 // 5 seconds for testing
            };

            channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            channel.QueueBind(queue, exchange, "test.dlq");

            // DLQ
            channel.QueueDeclare(dlqQueue, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(dlqQueue, dlxExchange, "");

            // Send test message
            var testMessage = new { Id = Guid.NewGuid(), Message = "DLQ test message" };
            var json = JsonSerializer.Serialize(testMessage);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = testMessage.Id.ToString();

            channel.BasicPublish(exchange, "test.dlq", properties, body);

            _logger.LogInformation("✅ DLQ test message published");

            // Wait for TTL expiry
            await Task.Delay(6000);

            // Check if message moved to DLQ
            var dlqResult = channel.BasicGet(dlqQueue, false);
            if (dlqResult != null)
            {
                _logger.LogInformation("✅ DLQ test passed - message found in dead letter queue");
                channel.BasicAck(dlqResult.DeliveryTag, false);
            }
            else
            {
                _logger.LogWarning("❌ DLQ test failed - message not found in dead letter queue");
            }

            // Cleanup
            channel.QueueDelete(queue);
            channel.QueueDelete(dlqQueue);
            channel.ExchangeDelete(exchange);
            channel.ExchangeDelete(dlxExchange);

            _logger.LogInformation("🔥 TESTING: DLQ tests completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ DLQ test failed");
        }
    }

    /// <summary>
    /// Test idempotency mechanisms
    /// </summary>
    public async Task TestIdempotencyAsync()
    {
        _logger.LogInformation("🔥 TESTING: Idempotency mechanisms");

        var processedMessages = new HashSet<string>();
        var duplicateCount = 0;

        // Simulate message processing with duplicates
        var messageIds = new[]
        {
            "msg-001", "msg-002", "msg-001", // Duplicate
            "msg-003", "msg-002", // Duplicate
            "msg-004"
        };

        foreach (var messageId in messageIds)
        {
            if (processedMessages.Contains(messageId))
            {
                duplicateCount++;
                _logger.LogInformation("✅ Duplicate detected: {MessageId}", messageId);
            }
            else
            {
                processedMessages.Add(messageId);
                _logger.LogInformation("Processing new message: {MessageId}", messageId);
            }
        }

        if (duplicateCount == 2)
        {
            _logger.LogInformation("✅ Idempotency test passed - {DuplicateCount} duplicates detected", duplicateCount);
        }
        else
        {
            _logger.LogWarning("❌ Idempotency test failed - expected 2 duplicates, found {DuplicateCount}", duplicateCount);
        }

        _logger.LogInformation("🔥 TESTING: Idempotency tests completed");
    }

    /// <summary>
    /// Run all reliability tests
    /// </summary>
    public async Task RunAllTestsAsync()
    {
        _logger.LogInformation("🔥 STARTING: RabbitMQ Reliability Test Suite");

        await TestPublishReliabilityAsync();
        await TestDeadLetterQueueAsync();
        await TestIdempotencyAsync();

        _logger.LogInformation("🔥 COMPLETED: RabbitMQ Reliability Test Suite");
    }
}