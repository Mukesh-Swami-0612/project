using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;

namespace Ecom.Reporting.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IAuditRepository _repository;

    public DashboardService(IAuditRepository repository) => _repository = repository;

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        // Use repository method for aggregation
        var actionCounts = await _repository.GetActionCountsAsync();

        return new DashboardSummaryDto
        {
            TotalProducts = actionCounts.GetValueOrDefault("Created", 0),
            PendingApprovals = actionCounts.GetValueOrDefault("Submitted", 0),
            PublishedProducts = actionCounts.GetValueOrDefault("Published", 0),
            RejectedProducts = actionCounts.GetValueOrDefault("Rejected", 0),
            LowStockAlerts = actionCounts.GetValueOrDefault("StockChanged", 0),
            GeneratedAt = DateTime.UtcNow
        };
    }
}
