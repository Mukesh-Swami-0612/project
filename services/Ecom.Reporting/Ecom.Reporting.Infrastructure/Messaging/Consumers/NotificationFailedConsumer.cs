using System.Text.Json;
using Ecom.Reporting.Domain.Entities;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

public class NotificationFailedConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.notification.failed";
    protected override string RoutingKey => "notification.failed";

    public NotificationFailedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<NotificationFailedConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var eventData = JsonSerializer.Deserialize<NotificationFailedEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize notification.failed event");
            return;
        }

        _logger.LogInformation(
            "Processing event {EventType} with EventId {EventId}",
            nameof(NotificationFailedEvent),
            eventData?.EventId);

        var notification = await context.Notifications
            .FindAsync(eventData.NotificationId);

        if (notification == null)
        {
            notification = new NotificationReadModel
            {
                NotificationId = eventData.NotificationId,
                Status = "Failed",
                Type = eventData.Type ?? "Unknown",
                Recipient = eventData.Recipient ?? "Unknown",
                FailureReason = eventData.FailureReason,
                RetryCount = eventData.RetryCount,
                CreatedAt = eventData.CreatedAt
            };
            context.Notifications.Add(notification);
        }
        else
        {
            notification.Status = "Failed";
            notification.FailureReason = eventData.FailureReason;
            notification.RetryCount = eventData.RetryCount;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Processed notification.failed for NotificationId: {NotificationId}", 
            eventData.NotificationId);
    }

    private class NotificationFailedEvent
    {
        public Guid EventId { get; init; }
        public string NotificationId { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Recipient { get; set; }
        public string? FailureReason { get; set; }
        public int RetryCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
