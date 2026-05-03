using System.Text.Json;
using System.Diagnostics;
using Ecom.Reporting.Application.Configuration;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Application.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecom.Reporting.Infrastructure.Messaging.Consumers;

/// <summary>
/// Subscribes to: ecom-events / user.action
/// Consumes UserActionEvent from Auth service for audit logging
/// </summary>
public class UserActionConsumer : RabbitMqConsumerBase
{
    protected override string QueueName => "reporting.user.action";
    protected override string RoutingKey => "user.action";

    public UserActionConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<UserActionConsumer> logger)
        : base(configuration, serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(string message)
    {
        using var scope = _serviceProvider.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<IdempotencyOptions>>().Value;

        var eventData = JsonSerializer.Deserialize<UserActionEvent>(message);
        
        if (eventData == null)
        {
            _logger.LogWarning("Failed to deserialize user.action event");
            return;
        }

        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(eventData.TraceId) && !string.IsNullOrEmpty(eventData.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(eventData.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(eventData.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = ReportingActivitySource.Instance.StartActivity(
                "ProcessUserAction",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = ReportingActivitySource.Instance.StartActivity("ProcessUserAction");
        }

        using (activity)
        {
            activity?.SetTag("event.id", eventData.EventId);
            activity?.SetTag("entity.id", eventData.EntityId);

            _logger.LogInformation(
                "Processing event {EventType} with EventId {EventId} for EntityId {EntityId}",
                nameof(UserActionEvent),
                eventData.EventId,
                eventData.EntityId);

        // Phase 2: Check idempotency with enforcement
        var alreadyProcessed = await idempotencyService.IsProcessedAsync(eventData.EventId);
        
        if (alreadyProcessed)
        {
            if (options.Enforce)
            {
                // Phase 2: Skip duplicate when enforcement is enabled
                _logger.LogInformation(
                    "Skipping duplicate EventId {EventId} (enforcement enabled)",
                    eventData.EventId);
                return;
            }
            else
            {
                // Phase 1: Log but continue processing
                _logger.LogInformation(
                    "Idempotency check for EventId {EventId}: ALREADY_PROCESSED (enforcement disabled, continuing)",
                    eventData.EventId);
            }
        }
        else
        {
            _logger.LogInformation(
                "Idempotency check for EventId {EventId}: NEW",
                eventData.EventId);
        }

        // Phase 2: Use transactional processing when enforcement is enabled
        if (options.Enforce)
        {
            try
            {
                await idempotencyService.ExecuteWithIdempotencyAsync(
                    eventData.EventId,
                    nameof(UserActionEvent),
                    async () =>
                    {
                        await auditService.WriteAsync(new AuditLogDto
                        {
                            EntityName = eventData.EntityName ?? "User",
                            EntityId = eventData.EntityId,
                            Action = eventData.Action ?? "Unknown",
                            EventType = eventData.EventType ?? "user.action",
                            SourceService = "AuthService",
                            CorrelationId = eventData.CorrelationId ?? Guid.NewGuid().ToString(),
                            CreatedAt = eventData.CreatedAt
                        });
                    });

                _logger.LogInformation(
                    "Transactionally processed user.action for EntityId: {EntityId}, Action: {Action}",
                    eventData.EntityId,
                    eventData.Action);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex,
                    "Failed to process user.action for EntityId: {EntityId}, will retry",
                    eventData.EntityId);
                throw; // Propagate for retry
            }
        }
        else
        {
            // Phase 1: Non-transactional processing (backward compatible)
            await auditService.WriteAsync(new AuditLogDto
            {
                EntityName = eventData.EntityName ?? "User",
                EntityId = eventData.EntityId,
                Action = eventData.Action ?? "Unknown",
                EventType = eventData.EventType ?? "user.action",
                SourceService = "AuthService",
                CorrelationId = eventData.CorrelationId ?? Guid.NewGuid().ToString(),
                CreatedAt = eventData.CreatedAt
            });

            _logger.LogInformation(
                "Processed user.action for EntityId: {EntityId}, Action: {Action}, Email: {Email}",
                eventData.EntityId,
                eventData.Action,
                eventData.Email);

            // Phase 1: Mark as processed after successful processing
            await idempotencyService.MarkProcessedAsync(eventData.EventId, nameof(UserActionEvent));
        }
        }
    }

    /// <summary>
    /// Internal event structure matching UserActionEvent from shared contracts
    /// </summary>
    private class UserActionEvent
    {
        public Guid EventId { get; init; }
        public string? EntityName { get; set; }
        public int? EntityId { get; set; }
        public string? Action { get; set; }
        public string? EventType { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Email { get; set; }
        public string? AdditionalInfo { get; set; }
        
        // 🔍 DISTRIBUTED TRACING: Optional trace context
        public string? TraceId { get; init; }
        public string? SpanId { get; init; }
    }
}
