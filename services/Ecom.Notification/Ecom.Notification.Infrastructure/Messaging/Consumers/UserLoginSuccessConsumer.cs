using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Application.Constants;
using Ecom.Notification.Application.Services;
using Ecom.Notification.Application.Telemetry;
using Ecom.Notification.Domain.Entities;
using Ecom.Notification.Domain.Events;
using Ecom.Notification.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Ecom.Notification.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumes user.login.success events and sends login notification emails
/// Only sends emails for suspicious logins (new IP or device)
/// </summary>
public class UserLoginSuccessConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserLoginSuccessConsumer> _logger;

    public UserLoginSuccessConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<UserLoginSuccessConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(UserLoginSuccessEvent message)
    {
        // 🔍 DISTRIBUTED TRACING: Restore trace context from event
        Activity? activity = null;
        if (!string.IsNullOrEmpty(message.TraceId) && !string.IsNullOrEmpty(message.SpanId))
        {
            var traceId = ActivityTraceId.CreateFromString(message.TraceId.AsSpan());
            var spanId = ActivitySpanId.CreateFromString(message.SpanId.AsSpan());
            var activityContext = new ActivityContext(traceId, spanId, ActivityTraceFlags.Recorded);
            
            activity = NotificationActivitySource.Instance.StartActivity(
                "ProcessUserLoginSuccess",
                ActivityKind.Consumer,
                activityContext);
        }
        else
        {
            activity = NotificationActivitySource.Instance.StartActivity("ProcessUserLoginSuccess");
        }

        using (activity)
        {
            activity?.SetTag("event.id", message?.EventId);
            activity?.SetTag("user.id", message?.UserId);

            using var scope = _scopeFactory.CreateScope();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var templateService = scope.ServiceProvider.GetRequiredService<EmailTemplateService>();
            var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            _logger.LogInformation(
                "Processing event {EventType} with EventId {EventId}",
                nameof(UserLoginSuccessEvent),
                message?.EventId);

            _logger.LogInformation(
                "NOTIFICATION_RECEIVED_USER_LOGIN | UserId: {UserId} | Email: {Email} | IP: {IpAddress} | CorrelationId: {CorrelationId}",
                message.UserId,
                message.Email,
                message.IpAddress,
                message.CorrelationId);

            try
            {
            bool isSuspicious = false;
            string suspiciousReason = string.Empty;

            // 🔥 STEP 1: Validate input data
            if (string.IsNullOrEmpty(message.IpAddress) || string.IsNullOrEmpty(message.UserAgent))
            {
                _logger.LogWarning(
                    "INVALID_LOGIN_DATA | UserId: {UserId} | IP: {IpAddress} | UA: {UserAgent}",
                    message.UserId,
                    message.IpAddress ?? "NULL",
                    message.UserAgent ?? "NULL");
                
                // Treat missing data as suspicious
                isSuspicious = true;
                suspiciousReason = "Missing IP address or device information";
            }
            else
            {
                // 🔥 STEP 2: Normalize device info (prevent false positives from version changes)
                var currentDevice = ExtractDeviceInfo(message.UserAgent);

                // 🔥 STEP 3: Check login history for suspicious activity
                var lastLogin = await dbContext.UserLoginHistory
                    .Where(x => x.UserId == message.UserId)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastLogin == null)
                {
                    // First login ever - suspicious
                    isSuspicious = true;
                    suspiciousReason = "First login detected";
                    _logger.LogInformation(
                        "SUSPICIOUS_LOGIN_DETECTED | UserId: {UserId} | Reason: FirstLogin",
                        message.UserId);
                }
                else if (lastLogin.IpAddress != message.IpAddress)
                {
                    // New IP address - suspicious
                    isSuspicious = true;
                    suspiciousReason = $"New IP address detected (Previous: {lastLogin.IpAddress}, Current: {message.IpAddress})";
                    _logger.LogInformation(
                        "SUSPICIOUS_LOGIN_DETECTED | UserId: {UserId} | Reason: NewIP | Previous: {PreviousIP} | Current: {CurrentIP}",
                        message.UserId,
                        lastLogin.IpAddress,
                        message.IpAddress);
                }
                else
                {
                    // 🔥 Compare normalized device types (not raw User-Agent)
                    var lastDevice = ExtractDeviceInfo(lastLogin.UserAgent);
                    
                    if (lastDevice != currentDevice)
                    {
                        // New device/browser - suspicious
                        isSuspicious = true;
                        suspiciousReason = $"New device detected (Previous: {lastDevice}, Current: {currentDevice})";
                        _logger.LogInformation(
                            "SUSPICIOUS_LOGIN_DETECTED | UserId: {UserId} | Reason: NewDevice | Previous: {PreviousDevice} | Current: {CurrentDevice}",
                            message.UserId,
                            lastDevice,
                            currentDevice);
                    }
                }
            }

            // 🔥 STEP 4: Save login history
            var loginHistory = new UserLoginHistory
            {
                UserId = message.UserId,
                IpAddress = message.IpAddress ?? "Unknown",
                UserAgent = message.UserAgent ?? "Unknown",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.UserLoginHistory.Add(loginHistory);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "LOGIN_HISTORY_SAVED | UserId: {UserId} | IP: {IpAddress}",
                message.UserId,
                message.IpAddress);

            // 🔥 STEP 3: Send email ONLY if suspicious
            if (!isSuspicious)
            {
                _logger.LogInformation(
                    "NORMAL_LOGIN_NO_EMAIL | UserId: {UserId} | Email: {Email}",
                    message.UserId,
                    message.Email);
                return; // ❌ Skip email for normal logins
            }

            // 🔥 STEP 4: Send suspicious login alert
            var loginTime = message.OccurredAt.ToString("yyyy-MM-dd HH:mm:ss UTC");
            
            // Load template and replace placeholders
            var template = templateService.LoadTemplate("login-alert.html");
            var body = templateService.ReplacePlaceholders(template, new Dictionary<string, string>
            {
                { "Time", loginTime },
                { "IpAddress", message.IpAddress ?? "Unknown" },
                { "Device", ExtractDeviceInfo(message.UserAgent ?? "Unknown") },
                { "Reason", suspiciousReason }
            });

            await notificationService.SendEventNotificationAsync(
                "USER_LOGIN_ALERT",
                EmailSubjects.LoginAlert,
                body,
                message.Email,
                message.CorrelationId
            );

            _logger.LogInformation(
                "SUSPICIOUS_LOGIN_EMAIL_SENT | UserId: {UserId} | Email: {Email} | Reason: {Reason} | CorrelationId: {CorrelationId}",
                message.UserId,
                message.Email,
                suspiciousReason,
                message.CorrelationId);
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _logger.LogError(
                    ex,
                    "NOTIFICATION_LOGIN_FAILED | UserId: {UserId} | Email: {Email} | Error: {Error}",
                    message.UserId,
                    message.Email,
                    ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Extract readable device information from User-Agent string
    /// </summary>
    private static string ExtractDeviceInfo(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        // Simple device detection
        if (userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            if (userAgent.Contains("iPhone")) return "iPhone";
            if (userAgent.Contains("iPad")) return "iPad";
            if (userAgent.Contains("Android")) return "Android Mobile";
            return "Mobile Device";
        }

        if (userAgent.Contains("Windows")) return "Windows PC";
        if (userAgent.Contains("Macintosh")) return "Mac";
        if (userAgent.Contains("Linux")) return "Linux PC";

        // Browser detection
        if (userAgent.Contains("Chrome")) return "Chrome Browser";
        if (userAgent.Contains("Firefox")) return "Firefox Browser";
        if (userAgent.Contains("Safari")) return "Safari Browser";
        if (userAgent.Contains("Edge")) return "Edge Browser";

        return "Unknown Device";
    }
}
