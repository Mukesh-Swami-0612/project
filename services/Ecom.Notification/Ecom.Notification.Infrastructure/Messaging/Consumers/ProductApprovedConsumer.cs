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
/// Consumes product.approved events and creates notifications
/// </summary>
public class ProductApprovedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductApprovedConsumer> _logger;

    public ProductApprovedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ProductApprovedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(ProductApprovedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = NotificationActivitySource.Instance.StartActivity(
                "ProcessProductApproved",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = NotificationActivitySource.Instance.StartActivity("ProcessProductApproved");
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
                nameof(ProductApprovedEvent),
                message?.EventId);

            _logger.LogInformation(
                "NOTIFICATION_RECEIVED_PRODUCT_APPROVED | ProductId: {ProductId} | ApprovedBy: {ApprovedBy} | CorrelationId: {CorrelationId}",
                message.ProductId,
                message.ApprovedBy,
                message.CorrelationId);

            try
            {
                // Load template and replace placeholders
                var template = templateService.LoadTemplate("product-approved.html");
                var body = templateService.ReplacePlaceholders(template, new Dictionary<string, string>
                {
                    { "ProductId", message.ProductId.ToString() }
                });

                await notificationService.SendEventNotificationAsync(
                    "PRODUCT_APPROVED",
                    EmailSubjects.ProductApproved,
                    body,
                    "admin@example.com", // TODO: Get product owner email
                    message.CorrelationId
                );

                _logger.LogInformation(
                    "NOTIFICATION_CREATED_FOR_APPROVAL | ProductId: {ProductId} | CorrelationId: {CorrelationId}",
                    message.ProductId,
                    message.CorrelationId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "NOTIFICATION_APPROVAL_FAILED | ProductId: {ProductId} | Error: {Error}",
                    message.ProductId,
                    ex.Message);
                throw;
            }
        }
    }
}
