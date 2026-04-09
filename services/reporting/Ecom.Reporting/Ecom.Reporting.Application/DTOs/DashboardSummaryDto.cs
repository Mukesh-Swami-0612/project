namespace Ecom.Reporting.Application.DTOs;

public class DashboardSummaryDto
{
    public int TotalProducts { get; set; }
    public int PendingApprovals { get; set; }
    public int PublishedProducts { get; set; }
    public int LowStockAlerts { get; set; }
    public int RejectedProducts { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
