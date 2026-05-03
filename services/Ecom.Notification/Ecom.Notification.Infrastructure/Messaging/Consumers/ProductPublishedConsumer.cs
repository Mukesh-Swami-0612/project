using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Application.Constants;
using Ecom.Notification.Application.Services;
using Ecom.Notification.Application.Telemetry;
using Ecom.Notification.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Notification.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes product.published events and creates notifications
/// </summary>
public class ProductPublishedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductPublishedConsumer> _logger;

    public ProductPublishedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductPublishedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ProductPublishedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = NotificationActivitySource.Instance.StartActivity(
                "ProcessProductPublished",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = NotificationActivitySource.Instance.StartActivity("ProcessProductPublished");
        }

        using (activity)
        {
            activity?.SetTag("event.id", message?.EventId);
            activity?.SetTag("product.id", message?.ProductId);

            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();

            _logger.LogInformation(
                "Processing event {EventType} with EventId {EventId}",
                nameof(ProductPublishedEvent),
                message?.EventId);

            _logger.LogInformation(
                "NOTIFICATION_RECEIVED_PRODUCT_PUBLISHED | ProductId: {ProductId} | SKU: {SKU} | Name: {Name} | CorrelationId: {CorrelationId}",
                message.ProductId,
                message.SKU,
                message.Name,
                message.CorrelationId);

            try
            {
                // Load template and replace placeholders
                var template = templateService.LoadTemplate("product-published.html");
                var body = templateService.ReplacePlaceholders(template, new Dictionary<string, string>
                {
                    { "ProductId", message.ProductId.ToString() },
                    { "PublishedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") }
                });

                await notificationService.SendEventNotificationAsync(
                    "PRODUCT_PUBLISHED",
                    EmailSubjects.ProductPublished,
                    body,
                    "admin@example.com", // TODO: Get product owner email
                    message.CorrelationId
                );

                _logger.LogInformation(
                    "NOTIFICATION_CREATED_FOR_PUBLISH | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
                    message.ProductId,
                    message.CorrelationId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "NOTIFICATION_PUBLISH_FAILED | ProductId: {ProductId} | Error: {Error}",
                    message.ProductId,
                    ex.Message);
                throw;
            }
        }
    }
}
