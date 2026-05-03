using System.Text.Json;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

public class NotificationSentConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.notification.sent";
    protected override string RoutingKey => "notification.sent";

    public NotificationSentConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<NotificationSentConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var eventData = JsonSerializer.Deserialize<NotificationSentEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize notification.sent event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId} for NotificationId {NotificationId}",
            nameof(NotificationSentEvent),
            eventData.EventId,
            eventData.NotificationId);

        var notification = await context.Notifications
            .FindAsync(eventData.NotificationId);

        if (notification == null)
        {
            notification = new NotificationReadModel
            {
                NotificationId = eventData.NotificationId,
                Status = "Sent",
                Type = eventData.Type ?? "Unknown",
                Recipient = eventData.Recipient ?? "Unknown",
                RetryCount = eventData.RetryCount,
                CreatedAt = eventData.CreatedAt,
                SentAt = DateTime.UtcNow
            };
            context.Notifications.Add(notification);
        }
        else
        {
            notification.Status = "Sent";
            notification.RetryCount = eventData.RetryCount;
            notification.SentAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Processed notification.sent for NotificationId: {NotificationId}", 
            eventData.NotificationId);
    }

    private class NotificationSentEvent
    {
        public Guid EventId { get; init; }
        public string NotificationId { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Recipient { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
