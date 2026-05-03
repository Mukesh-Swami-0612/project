using Ecom.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Notification.Infrastructure.Services;

/// <summary>
/// Background service that automatically cleans up old logs
/// Runs daily to delete logs older than 30 days
/// Prevents database from growing indefinitely
/// </summary>
public class LogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly int _retentionDays;

    public LogCleanupService(
        IServiceScopeFactory scopeFactory, 
        ILogger<LogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = 30; // Keep logs for 30 days
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LOG_CLEANUP_SERVICE_STARTED | RetentionDays: {RetentionDays}", _retentionDays);

        // Wait 1 hour before first cleanup (let system stabilize)
        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupLogsAsync();

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupLogsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

            _logger.LogInformation(
                "LOG_CLEANUP_STARTED | CutoffDate: {CutoffDate} | RetentionDays: {RetentionDays}",
                cutoffDate, _retentionDays);

            // Delete old logs
            var deletedCount = await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM NotificationLogs WHERE LoggedAt < {0}", 
                cutoffDate);

            _logger.LogInformation(
                "LOG_CLEANUP_COMPLETED | DeletedCount: {DeletedCount} | CutoffDate: {CutoffDate}",
                deletedCount, cutoffDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "LOG_CLEANUP_FAILED | Error: {Error}", 
                ex.Message);
        }
    }
}
