using Ecom.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Notification.Infrastructure.Services;

/// <summary>
/// Background service that monitors logs and triggers alerts
/// Detects anomalies and system issues automatically
/// Runs every 5 minutes to check for problems
/// </summary>
public class AlertMonitoringService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertMonitoringService> _logger;

    // Alert thresholds
    private const int ErrorThreshold = 10;           // Max errors in 5 minutes
    private const double SuccessRateThreshold = 0.8; // Min 80% success rate
    private const int MinSampleSize = 10;            // Min notifications to calculate rate

    public AlertMonitoringService(
        IServiceScopeFactory scopeFactory,
        ILogger<AlertMonitoringService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ALERT_MONITORING_SERVICE_STARTED | CheckInterval: 5min");

        // Wait 2 minutes before first check (let system stabilize)
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAlertsAsync();

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private async Task CheckAlertsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var since = DateTime.UtcNow.AddMinutes(-5);

            // Check 1: High error rate
            await CheckHighErrorRateAsync(context, since);

            // Check 2: Low success rate
            await CheckLowSuccessRateAsync(context, since);

            // Check 3: Service health (no activity)
            await CheckServiceHealthAsync(context, since);

            // Check 4: High retry rate
            await CheckHighRetryRateAsync(context, since);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "ALERT_MONITORING_CHECK_FAILED | Error: {Error}", 
                ex.Message);
        }
    }

    /// <summary>
    /// Alert if too many errors in the last 5 minutes
    /// </summary>
    private async Task CheckHighErrorRateAsync(NotificationDbContext context, DateTime since)
    {
        var errorCount = await context.NotificationLogs
            .CountAsync(x => x.Level == "Error" && x.LoggedAt >= since);

        if (errorCount > ErrorThreshold)
        {
            _logger.LogError(
                "ALERT_HIGH_ERROR_RATE | ErrorCount: {ErrorCount} | Threshold: {Threshold} | Window: 5min",
                errorCount, ErrorThreshold);

            // 🔥 TODO: Send alert email/SMS/Slack notification
            // await _alertService.SendAlertAsync("High Error Rate", $"Detected {errorCount} errors in last 5 minutes");
        }
    }

    /// <summary>
    /// Alert if success rate drops below threshold
    /// </summary>
    private async Task CheckLowSuccessRateAsync(NotificationDbContext context, DateTime since)
    {
        var logs = await context.NotificationLogs
            .Where(x => x.LoggedAt >= since)
            .Select(x => x.Message)
            .ToListAsync();

        var sentCount = logs.Count(x => x.Contains("EMAIL_SENT"));
        var failedCount = logs.Count(x => x.Contains("EMAIL_FAILED_MAX_RETRIES"));

        var totalCount = sentCount + failedCount;

        // Only calculate if we have enough samples
        if (totalCount >= MinSampleSize)
        {
            var successRate = (double)sentCount / totalCount;

            if (successRate < SuccessRateThreshold)
            {
                _logger.LogError(
                    "ALERT_LOW_SUCCESS_RATE | SuccessRate: {SuccessRate:P} | Threshold: {Threshold:P} | Sent: {Sent} | Failed: {Failed} | Window: 5min",
                    successRate, SuccessRateThreshold, sentCount, failedCount);

                // 🔥 TODO: Send alert
                // await _alertService.SendAlertAsync("Low Success Rate", $"Success rate dropped to {successRate:P}");
            }
        }
    }

    /// <summary>
    /// Alert if no activity detected (service might be down)
    /// </summary>
    private async Task CheckServiceHealthAsync(NotificationDbContext context, DateTime since)
    {
        var logCount = await context.NotificationLogs
            .CountAsync(x => x.LoggedAt >= since);

        if (logCount == 0)
        {
            _logger.LogWarning(
                "ALERT_NO_ACTIVITY | Window: 5min | Possible service outage");

            // 🔥 TODO: Send alert
            // await _alertService.SendAlertAsync("No Activity Detected", "No logs in last 5 minutes - service might be down");
        }
    }

    /// <summary>
    /// Alert if too many retries (indicates email service issues)
    /// </summary>
    private async Task CheckHighRetryRateAsync(NotificationDbContext context, DateTime since)
    {
        var logs = await context.NotificationLogs
            .Where(x => x.LoggedAt >= since)
            .Select(x => x.Message)
            .ToListAsync();

        var retryCount = logs.Count(x => x.Contains("EMAIL_RETRY_SCHEDULED"));
        var sentCount = logs.Count(x => x.Contains("EMAIL_SENT"));

        var totalCount = retryCount + sentCount;

        if (totalCount >= MinSampleSize)
        {
            var retryRate = (double)retryCount / totalCount;

            // Alert if more than 50% require retries
            if (retryRate > 0.5)
            {
                _logger.LogWarning(
                    "ALERT_HIGH_RETRY_RATE | RetryRate: {RetryRate:P} | Retries: {Retries} | Total: {Total} | Window: 5min",
                    retryRate, retryCount, totalCount);

                // 🔥 TODO: Send alert
                // await _alertService.SendAlertAsync("High Retry Rate", $"Retry rate is {retryRate:P} - email service might be slow");
            }
        }
    }
}
