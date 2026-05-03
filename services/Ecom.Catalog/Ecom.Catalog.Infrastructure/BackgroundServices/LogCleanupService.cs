using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Catalog.Infrastructure.BackgroundServices;

public class LogCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LogCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);

    public LogCleanupService(
        IServiceProvider serviceProvider,
        ILogger<LogCleanupService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LOG_CLEANUP_SERVICE_STARTED | Interval: {Interval}h", _cleanupInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
                await CleanupOldLogsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("LOG_CLEANUP_SERVICE_STOPPING");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LOG_CLEANUP_FAILED | Error: {Error}", ex.Message);
            }
        }
    }

    private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var retentionDays = 30;
        if (int.TryParse(_configuration["LogRetentionDays"], out var configuredDays))
        {
            retentionDays = configuredDays;
        }
        
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        _logger.LogInformation("LOG_CLEANUP_STARTED | RetentionDays: {Days} | CutoffDate: {Date}", 
            retentionDays, cutoffDate);

        try
        {
            // Clean up Serilog Logs table
            var logsDeleted = await dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM Logs WHERE TimeStamp < {0}", cutoffDate);

            // Clean up old audit logs (keep longer - 90 days)
            var auditCutoffDate = DateTime.UtcNow.AddDays(-90);
            var auditLogsDeleted = await dbContext.Database.ExecuteSqlRawAsync(
                "DELETE FROM AuditLogs WHERE Timestamp < {0}", auditCutoffDate);

            _logger.LogInformation(
                "LOG_CLEANUP_COMPLETED | LogsDeleted: {LogsDeleted} | AuditLogsDeleted: {AuditLogsDeleted} | Time: {Time}",
                logsDeleted, auditLogsDeleted, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LOG_CLEANUP_ERROR | Error: {Error}", ex.Message);
        }
    }
}
