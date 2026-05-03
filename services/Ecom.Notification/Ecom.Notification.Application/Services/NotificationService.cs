using Ecom.Notification.Application.DTOs;
using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Domain.Entities;
using Ecom.Notification.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Ecom.Notification.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly INotificationRepository _repository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService, 
        INotificationRepository repository,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _repository = repository;
        _logger = logger;
    }

    // ✅ EVENT-DRIVEN NOTIFICATION (NEW)
    public async Task SendEventNotificationAsync(string type, string subject, string body, string toEmail, string? correlationId)
    {
        // 🔥 IDEMPOTENCY CHECK: Prevent duplicate notifications
        if (!string.IsNullOrEmpty(correlationId))
        {
            var exists = await _repository.ExistsAsync(correlationId, type);
            if (exists)
            {
                _logger.LogInformation(
                    "NOTIFICATION_DUPLICATE_SKIPPED | Type: {Type} | Email: {Email} | CorrelationId: {CorrelationId}",
                    type, toEmail, correlationId);
                return; // ✅ Idempotent - skip duplicate
            }
        }

        var notification = new NotificationMessage
        {
            Type = type,
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            CorrelationId = correlationId,
            Status = "Pending",
            ScheduledAt = null // Send immediately via worker
        };

        try
        {
            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();

            _logger.LogInformation(
                "NOTIFICATION_CREATED | Type: {Type} | Email: {Email} | NotificationId: {NotificationId} | CorrelationId: {CorrelationId}",
                type, toEmail, notification.Id, correlationId);
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("UQ_Notification_Idempotency") == true || 
                                    ex.Message.Contains("UQ_Notification_Idempotency"))
        {
            // 🔥 Duplicate constraint violation - safe to ignore
            _logger.LogWarning(
                "NOTIFICATION_DUPLICATE_CONSTRAINT | Type: {Type} | Email: {Email} | CorrelationId: {CorrelationId}",
                type, toEmail, correlationId);
        }
    }

    // ✅ SEND NOW
    public async Task<bool> SendNowAsync(NotificationRequest request)
    {
        var message = new NotificationMessage
        {
            Type = "MANUAL",
            ToEmail = request.ToEmail,
            Subject = request.Subject,
            Body = request.Body,
            Status = "Pending",
            ScheduledAt = null
        };

        bool result = await SendEmailWithRetryAsync(message);

        await _repository.AddAsync(message);
        await _repository.SaveChangesAsync();

        return result;
    }

    // ✅ SCHEDULE ONLY
    public async Task<bool> ScheduleAsync(ScheduleNotificationRequest request)
    {
        var scheduledTimeIst = request.ScheduledAtIST;

        if (!scheduledTimeIst.HasValue)
        {
            _logger.LogWarning("SCHEDULE_VALIDATION_FAILED | Reason: ScheduledAtIST is required");
            return false;
        }

        var scheduledTimeUtc = ConvertToUtcFromIstInput(scheduledTimeIst.Value);

        if (scheduledTimeUtc <= DateTime.UtcNow.AddMinutes(-1))
        {
            _logger.LogWarning("SCHEDULE_VALIDATION_FAILED | Reason: Cannot schedule in the past | ScheduledTime: {ScheduledTime}", scheduledTimeUtc);
            return false;
        }

        var message = new NotificationMessage
        {
            Type = "SCHEDULED",
            ToEmail = request.ToEmail,
            Subject = request.Subject,
            Body = request.Body,
            Status = "Pending",
            ScheduledAt = scheduledTimeUtc
        };

        await _repository.AddAsync(message);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "NOTIFICATION_SCHEDULED | NotificationId: {NotificationId} | Email: {Email} | ScheduledAt: {ScheduledAt}",
            message.Id, request.ToEmail, scheduledTimeUtc);
        
        return true;
    }

    // ✅ PROCESS EXISTING (Called by Worker)
    public async Task<bool> ProcessScheduledAsync(NotificationMessage message)
    {
        bool result = await SendEmailWithRetryAsync(message);
        await _repository.SaveChangesAsync();
        return result;
    }

    // 🛠️ INTERNAL HELPER: Send Email with Exponential Backoff Retry
    private async Task<bool> SendEmailWithRetryAsync(NotificationMessage message)
    {
        const int maxRetries = 3;
        bool result = false;

        try
        {
            result = await _emailService.SendEmailAsync(
                message.ToEmail,
                message.Subject,
                message.Body
            );

            if (result)
            {
                // ✅ Success
                message.IsSent = true;
                message.Status = "Sent";
                message.SentAt = DateTime.UtcNow;
                message.NextRetryAt = null;
                
                _logger.LogInformation(
                    "EMAIL_SENT | NotificationId: {NotificationId} | Email: {Email} | RetryCount: {RetryCount}",
                    message.Id, message.ToEmail, message.RetryCount);
            }
            else
            {
                // ❌ Failed - apply exponential backoff
                message.RetryCount++;

                if (message.RetryCount >= maxRetries)
                {
                    // 🔥 Max retries reached - mark as failed
                    message.Status = "Failed";
                    message.NextRetryAt = null;
                    
                    _logger.LogError(
                        "EMAIL_FAILED_MAX_RETRIES | NotificationId: {NotificationId} | Email: {Email} | RetryCount: {RetryCount}",
                        message.Id, message.ToEmail, message.RetryCount);
                }
                else
                {
                    // 🔄 Schedule retry with exponential backoff
                    message.Status = "Pending";
                    var delaySeconds = Math.Pow(2, message.RetryCount); // 2^1=2s, 2^2=4s, 2^3=8s
                    message.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                    
                    _logger.LogWarning(
                        "EMAIL_RETRY_SCHEDULED | NotificationId: {NotificationId} | Email: {Email} | RetryCount: {RetryCount} | NextRetryAt: {NextRetryAt} | DelaySeconds: {DelaySeconds}",
                        message.Id, message.ToEmail, message.RetryCount, message.NextRetryAt, delaySeconds);
                }
            }
        }
        catch (Exception ex)
        {
            // ❌ Exception occurred - apply exponential backoff
            message.RetryCount++;

            if (message.RetryCount >= maxRetries)
            {
                // 🔥 Max retries reached - mark as failed
                message.Status = "Failed";
                message.NextRetryAt = null;
                
                _logger.LogError(ex,
                    "EMAIL_EXCEPTION_MAX_RETRIES | NotificationId: {NotificationId} | Email: {Email} | RetryCount: {RetryCount} | Error: {Error}",
                    message.Id, message.ToEmail, message.RetryCount, ex.Message);
            }
            else
            {
                // 🔄 Schedule retry with exponential backoff
                message.Status = "Pending";
                var delaySeconds = Math.Pow(2, message.RetryCount); // 2^1=2s, 2^2=4s, 2^3=8s
                message.NextRetryAt = DateTime.UtcNow.AddSeconds(delaySeconds);
                
                _logger.LogWarning(ex,
                    "EMAIL_EXCEPTION_RETRY_SCHEDULED | NotificationId: {NotificationId} | Email: {Email} | RetryCount: {RetryCount} | NextRetryAt: {NextRetryAt} | DelaySeconds: {DelaySeconds} | Error: {Error}",
                    message.Id, message.ToEmail, message.RetryCount, message.NextRetryAt, delaySeconds, ex.Message);
            }
        }

        return result;
    }

    private static DateTime ConvertToUtcFromIstInput(DateTime scheduledAt)
    {
        var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        var istDateTime = DateTime.SpecifyKind(scheduledAt, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(istDateTime, istZone);
    }
}
