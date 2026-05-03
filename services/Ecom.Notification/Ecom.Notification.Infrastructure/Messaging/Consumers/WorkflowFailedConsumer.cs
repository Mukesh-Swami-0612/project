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
/// Consumes workflow.failed events and creates notifications
/// </summary>
public class WorkflowFailedConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowFailedConsumer> _logger;

    public WorkflowFailedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowFailedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(WorkflowFailedEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = NotificationActivitySource.Instance.StartActivity(
                "ProcessWorkflowFailed",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = NotificationActivitySource.Instance.StartActivity("ProcessWorkflowFailed");
        }

        using (activity)
        {
            activity?.SetTag("event.id", message?.EventId);
            activity?.SetTag("workflow.id", message?.WorkflowId);
            activity?.SetTag("product.id", message?.ProductId);

            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();

            _logger.LogInformation(
                "Processing event {EventType} with EventId {EventId}",
                nameof(WorkflowFailedEvent),
                message?.EventId);

            _logger.LogInformation(
                "NOTIFICATION_RECEIVED_WORKFLOW_FAILED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | Error: {Error} | CorrelationId: {CorrelationId}",
                message.WorkflowId,
                message.ProductId,
                message.Error,
                message.CorrelationId);

            try
            {
                // Load template and replace placeholders
                var template = templateService.LoadTemplate("workflow-failed.html");
                var body = templateService.ReplacePlaceholders(template, new Dictionary<string, string>
                {
                    { "WorkflowId", message.WorkflowId.ToString() },
                    { "FailedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                    { "Error", message.Error ?? "Unknown error" }
                });

                await notificationService.SendEventNotificationAsync(
                    "WORKFLOW_FAILED",
                    EmailSubjects.WorkflowFailed,
                    body,
                    "admin@example.com", // TODO: Get workflow owner email
                    message.CorrelationId
                );

                _logger.LogInformation(
                    "NOTIFICATION_CREATED_FOR_WORKFLOW_FAILURE | WorkflowId: {WorkflowId} | CorrelationId: {CorrelationId}",
                    message.WorkflowId,
                    message.CorrelationId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "NOTIFICATION_WORKFLOW_FAILURE_FAILED | WorkflowId: {WorkflowId} | Error: {Error}",
                    message.WorkflowId,
                    ex.Message);
                throw;
            }
        }
    }
}
