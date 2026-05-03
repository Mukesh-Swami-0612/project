using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Infrastructure.BackgroundServices;

public class ReportingLogCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportingLogCleanupService> _logger;

    public ReportingLogCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReportingLogCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 1 hour before first cleanup
        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "REPORTING_LOG_CLEANUP_FAILED | Error during log cleanup");
            }

            // Run cleanup every 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CleanupAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-30);

        var deleted = await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM ReportingLogs WHERE LoggedAt < {0}", cutoff);

        _logger.LogInformation("REPORTING_LOG_CLEANUP | Deleted: {Count} | Cutoff: {Cutoff}", 
            deleted, cutoff);
    }
}
