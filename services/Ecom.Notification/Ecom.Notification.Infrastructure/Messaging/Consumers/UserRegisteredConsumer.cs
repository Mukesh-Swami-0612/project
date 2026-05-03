using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Application.Constants;
using Ecom.Notification.Application.Services;
using Ecom.Notification.Application.Utilities;
using Ecom.Notification.Application.Telemetry;
using Ecom.Notification.Domain.Events;
using Ecom.Shared.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Diagnostics;

namespace Ecom.Notification.Infrastructure.Messaging.Consumers;

/// <summary>
/// 🔥 RESILIENT: Enhanced consumer with idempotency and error handling
/// </summary>
public class UserRegisteredConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserRegisteredConsumer> _logger;
    private readonly IdempotencyService _idempotencyService;

    public UserRegisteredConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<UserRegisteredConsumer> logger,
        IdempotencyService idempotencyService)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _idempotencyService = idempotencyService;
    }

    public async Task<bool> HandleAsync(UserRegisteredEvent message, string messageId)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = NotificationActivitySource.Instance.StartActivity(
                "ProcessUserRegistered",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = NotificationActivitySource.Instance.StartActivity("ProcessUserRegistered");
        }

        using (activity)
        {
            activity?.SetTag("event.id", message?.EventId);
            activity?.SetTag("user.id", message?.UserId);

            using var scope = _scopeFactory.CreateScope();
            var idempotencyService = scope.ServiceProvider.GetRequiredService<IIdempotencyService>();

            // Phase 1: Check idempotency (tracking only, no blocking)
            var alreadyProcessed = await idempotencyService.IsProcessedAsync(message.EventId);
            _logger.LogInformation(
                "Idempotency check for EventId {EventId}: {Result}",
                message.EventId,
                alreadyProcessed ? "ALREADY_PROCESSED" : "NEW");

            // 🔥 IDEMPOTENCY: Check if already processed
            if (await _idempotencyService.IsProcessedAsync(messageId))
            {
                _logger.LogInformation("Duplicate UserRegisteredEvent {MessageId} for UserId {UserId}, skipping", 
                    messageId, message.UserId);
                return true;
            }

            // 🔥 IDEMPOTENCY: Acquire processing lock
            if (!await _idempotencyService.TryAcquireProcessingLockAsync(messageId))
            {
                _logger.LogWarning("UserRegisteredEvent {MessageId} is already being processed", messageId);
                return false; // Will retry later
            }

            try
            {
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();

                _logger.LogInformation(
                    "Processing event {EventType} with EventId {EventId}",
                    nameof(UserRegisteredEvent),
                    message?.EventId);

                _logger.LogInformation(
                    "NOTIFICATION_PROCESSING_USER_REGISTERED | MessageId: {MessageId} | UserId: {UserId} | Email: {Email} | CorrelationId: {CorrelationId}",
                    messageId,
                    message.UserId,
                    LogSanitizer.MaskEmail(message.Email),
                    message.CorrelationId);

                // 🔥 BUSINESS LOGIC: Process the notification
                var template = templateService.LoadTemplate("welcome.html");
                var body = templateService.ReplacePlaceholders(template, new Dictionary<string, string>
                {
                    { "Name", message.Name }
                });

                await notificationService.SendEventNotificationAsync(
                    "USER_REGISTERED",
                    EmailSubjects.Welcome,
                    body,
                    message.Email,
                    message.CorrelationId
                );

                // 🔥 IDEMPOTENCY: Mark as successfully processed
                await _idempotencyService.MarkAsProcessedAsync(messageId);

                // Phase 1: Mark EventId as processed
                await idempotencyService.MarkProcessedAsync(message.EventId, nameof(UserRegisteredEvent));

                _logger.LogInformation(
                    "NOTIFICATION_COMPLETED_USER_REGISTERED | MessageId: {MessageId} | UserId: {UserId} | CorrelationId: {CorrelationId}",
                    messageId,
                    message.UserId,
                    message.CorrelationId);

                return true;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(ex,
                    "NOTIFICATION_FAILED_USER_REGISTERED | MessageId: {MessageId} | UserId: {UserId} | Error: {Error}",
                    messageId,
                    message.UserId,
                    ex.Message);
                
                return false; // Will trigger retry
            }
            finally
            {
                // 🔥 IDEMPOTENCY: Always release processing lock
                await _idempotencyService.ReleaseProcessingLockAsync(messageId);
            }
        }
    }

    // Legacy method for backward compatibility
    public async Task HandleAsync(UserRegisteredEvent message)
    {
        var messageId = $"user-registered-{message.UserId}-{message.CorrelationId}";
        await HandleAsync(message, messageId);
    }
}
