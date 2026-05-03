using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecom.Reporting.Application.Services;

public class ReportingService : IReportingService
{
    private readonly ReportingDbContext _context;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(ReportingDbContext context, ILogger<ReportingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationReportDto> GetNotificationReportAsync(ReportQuery query)
    {
        _logger.LogInformation(
            "REPORT_NOTIFICATION_SUMMARY | From: {From} | To: {To}",
            query.From?.ToString("yyyy-MM-dd") ?? "N/A",
            query.To?.ToString("yyyy-MM-dd") ?? "N/A");

        var data = _context.Notifications.AsQueryable();

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var total = await data.CountAsync();
        var sent = await data.CountAsync(x => x.Status == "Sent");
        var failed = await data.CountAsync(x => x.Status == "Failed");
        var pending = await data.CountAsync(x => x.Status == "Pending");

        return new NotificationReportDto
        {
            Total = total,
            Sent = sent,
            Failed = failed,
            Pending = pending,
            SuccessRate = total == 0 ? 0 : Math.Round((double)sent / total * 100, 2)
        };
    }

    public async Task<WorkflowReportDto> GetWorkflowReportAsync(ReportQuery query)
    {
        _logger.LogInformation(
            "REPORT_WORKFLOW_SUMMARY | From: {From} | To: {To}",
            query.From?.ToString("yyyy-MM-dd") ?? "N/A",
            query.To?.ToString("yyyy-MM-dd") ?? "N/A");

        var data = _context.Workflows.AsQueryable();

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var total = await data.CountAsync();
        var completed = await data.CountAsync(x => x.Status == "Completed");
        var inProgress = await data.CountAsync(x => x.Status == "InProgress");
        var failed = await data.CountAsync(x => x.Status == "Failed");
        var cancelled = await data.CountAsync(x => x.Status == "Cancelled");

        return new WorkflowReportDto
        {
            Total = total,
            Completed = completed,
            InProgress = inProgress,
            Failed = failed,
            Cancelled = cancelled,
            CompletionRate = total == 0 ? 0 : Math.Round((double)completed / total * 100, 2)
        };
    }

    public async Task<ProductReportDto> GetProductReportAsync(ReportQuery query)
    {
        _logger.LogInformation(
            "REPORT_PRODUCT_SUMMARY | From: {From} | To: {To}",
            query.From?.ToString("yyyy-MM-dd") ?? "N/A",
            query.To?.ToString("yyyy-MM-dd") ?? "N/A");

        var data = _context.Products.AsQueryable();

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var total = await data.CountAsync();
        var published = await data.CountAsync(x => x.Status == "Published");
        var draft = await data.CountAsync(x => x.Status == "Draft");
        var pendingApproval = await data.CountAsync(x => x.Status == "PendingApproval");
        var rejected = await data.CountAsync(x => x.Status == "Rejected");
        var lowStock = await data.CountAsync(x => x.IsLowStock);

        return new ProductReportDto
        {
            Total = total,
            Published = published,
            Draft = draft,
            PendingApproval = pendingApproval,
            Rejected = rejected,
            LowStock = lowStock,
            PublishRate = total == 0 ? 0 : Math.Round((double)published / total * 100, 2)
        };
    }


    public async Task<List<DailyNotificationTrendDto>> GetNotificationTrendsAsync(ReportQuery query)
    {
        var from = query.From ?? DateTime.UtcNow.AddDays(-7);
        var to = query.To ?? DateTime.UtcNow;

        _logger.LogInformation(
            "REPORT_NOTIFICATION_TRENDS | Range: {From} - {To}",
            from.ToString("yyyy-MM-dd"),
            to.ToString("yyyy-MM-dd"));

        var data = await _context.Notifications
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new DailyNotificationTrendDto
            {
                Date = g.Key,
                Sent = g.Count(x => x.Status == "Sent"),
                Failed = g.Count(x => x.Status == "Failed"),
                Pending = g.Count(x => x.Status == "Pending")
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing dates for complete chart data
        var result = new List<DailyNotificationTrendDto>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var existing = data.FirstOrDefault(x => x.Date == date);
            result.Add(existing ?? new DailyNotificationTrendDto
            {
                Date = date,
                Sent = 0,
                Failed = 0,
                Pending = 0
            });
        }

        return result;
    }

    public async Task<List<DailyWorkflowTrendDto>> GetWorkflowTrendsAsync(ReportQuery query)
    {
        var from = query.From ?? DateTime.UtcNow.AddDays(-7);
        var to = query.To ?? DateTime.UtcNow;

        _logger.LogInformation(
            "REPORT_WORKFLOW_TRENDS | Range: {From} - {To}",
            from.ToString("yyyy-MM-dd"),
            to.ToString("yyyy-MM-dd"));

        var data = await _context.Workflows
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new DailyWorkflowTrendDto
            {
                Date = g.Key,
                Completed = g.Count(x => x.Status == "Completed"),
                InProgress = g.Count(x => x.Status == "InProgress"),
                Failed = g.Count(x => x.Status == "Failed")
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing dates for complete chart data
        var result = new List<DailyWorkflowTrendDto>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var existing = data.FirstOrDefault(x => x.Date == date);
            result.Add(existing ?? new DailyWorkflowTrendDto
            {
                Date = date,
                Completed = 0,
                InProgress = 0,
                Failed = 0
            });
        }

        return result;
    }

    public async Task<List<DailyProductTrendDto>> GetProductTrendsAsync(ReportQuery query)
    {
        var from = query.From ?? DateTime.UtcNow.AddDays(-7);
        var to = query.To ?? DateTime.UtcNow;

        _logger.LogInformation(
            "REPORT_PRODUCT_TRENDS | Range: {From} - {To}",
            from.ToString("yyyy-MM-dd"),
            to.ToString("yyyy-MM-dd"));

        var data = await _context.Products
            .Where(x => x.CreatedAt >= from && x.CreatedAt <= to)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new DailyProductTrendDto
            {
                Date = g.Key,
                Published = g.Count(x => x.Status == "Published"),
                Draft = g.Count(x => x.Status == "Draft"),
                PendingApproval = g.Count(x => x.Status == "PendingApproval"),
                Rejected = g.Count(x => x.Status == "Rejected")
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Fill missing dates for complete chart data
        var result = new List<DailyProductTrendDto>();
        for (var date = from.Date; date <= to.Date; date = date.AddDays(1))
        {
            var existing = data.FirstOrDefault(x => x.Date == date);
            result.Add(existing ?? new DailyProductTrendDto
            {
                Date = date,
                Published = 0,
                Draft = 0,
                PendingApproval = 0,
                Rejected = 0
            });
        }

        return result;
    }


    public async Task<List<FailureAnalysisDto>> GetNotificationFailureAnalysisAsync(ReportQuery query)
    {
        _logger.LogInformation("REPORT_FAILURE_ANALYSIS | Type: Notifications");

        var data = _context.Notifications
            .Where(x => x.Status == "Failed" && x.FailureReason != null);

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var totalFailed = await data.CountAsync();

        var analysis = await data
            .GroupBy(x => x.FailureReason)
            .Select(g => new FailureAnalysisDto
            {
                Reason = g.Key ?? "Unknown",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Calculate percentages
        foreach (var item in analysis)
        {
            item.Percentage = totalFailed == 0 ? 0 : Math.Round((double)item.Count / totalFailed * 100, 2);
        }

        return analysis;
    }

    public async Task<List<FailureAnalysisDto>> GetWorkflowFailureAnalysisAsync(ReportQuery query)
    {
        _logger.LogInformation("REPORT_FAILURE_ANALYSIS | Type: Workflows");

        var data = _context.Workflows
            .Where(x => x.Status == "Failed" && x.FailureReason != null);

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var totalFailed = await data.CountAsync();

        var analysis = await data
            .GroupBy(x => x.FailureReason)
            .Select(g => new FailureAnalysisDto
            {
                Reason = g.Key ?? "Unknown",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Calculate percentages
        foreach (var item in analysis)
        {
            item.Percentage = totalFailed == 0 ? 0 : Math.Round((double)item.Count / totalFailed * 100, 2);
        }

        return analysis;
    }

    public async Task<List<RejectionAnalysisDto>> GetProductRejectionAnalysisAsync(ReportQuery query)
    {
        _logger.LogInformation("REPORT_REJECTION_ANALYSIS | Type: Products");

        var data = _context.Products
            .Where(x => x.Status == "Rejected" && x.RejectionReason != null);

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var totalRejected = await data.CountAsync();

        var analysis = await data
            .GroupBy(x => x.RejectionReason)
            .Select(g => new RejectionAnalysisDto
            {
                Reason = g.Key ?? "Unknown",
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Calculate percentages
        foreach (var item in analysis)
        {
            item.Percentage = totalRejected == 0 ? 0 : Math.Round((double)item.Count / totalRejected * 100, 2);
        }

        return analysis;
    }


    public async Task<List<TopFailureEntityDto>> GetTopWorkflowFailuresAsync(ReportQuery query)
    {
        _logger.LogInformation("REPORT_HOTSPOT_DETECTION | Type: Workflows");

        var data = _context.Workflows
            .Where(x => x.Status == "Failed");

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var grouped = await data
            .GroupBy(x => new { x.WorkflowId, x.WorkflowType })
            .Select(g => new
            {
                g.Key.WorkflowId,
                g.Key.WorkflowType,
                FailureCount = g.Count(),
                MostCommonReason = g
                    .Where(w => w.FailureReason != null)
                    .GroupBy(w => w.FailureReason)
                    .OrderByDescending(r => r.Count())
                    .Select(r => r.Key)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.FailureCount)
            .Take(10)
            .ToListAsync();

        return grouped.Select(x => new TopFailureEntityDto
        {
            EntityId = x.WorkflowId,
            EntityName = x.WorkflowType,
            FailureCount = x.FailureCount,
            MostCommonReason = x.MostCommonReason
        }).ToList();
    }

    public async Task<List<TopRejectedProductDto>> GetTopRejectedProductsAsync(ReportQuery query)
    {
        _logger.LogInformation("REPORT_HOTSPOT_DETECTION | Type: Products");

        var data = _context.Products
            .Where(x => x.Status == "Rejected");

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var grouped = await data
            .GroupBy(x => new { x.ProductId, x.Name })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Name,
                RejectionCount = g.Count(),
                MostCommonReason = g
                    .Where(p => p.RejectionReason != null)
                    .GroupBy(p => p.RejectionReason)
                    .OrderByDescending(r => r.Count())
                    .Select(r => r.Key)
                    .FirstOrDefault()
            })
            .OrderByDescending(x => x.RejectionCount)
            .Take(10)
            .ToListAsync();

        return grouped.Select(x => new TopRejectedProductDto
        {
            ProductId = x.ProductId,
            ProductName = x.Name,
            RejectionCount = x.RejectionCount,
            MostCommonReason = x.MostCommonReason
        }).ToList();
    }


    public async Task<NotificationPerformanceDto> GetNotificationPerformanceAsync(ReportQuery query)
    {
        _logger.LogInformation(
            "REPORT_NOTIFICATION_PERFORMANCE | From: {From} | To: {To}",
            query.From?.ToString("yyyy-MM-dd") ?? "N/A",
            query.To?.ToString("yyyy-MM-dd") ?? "N/A");

        var data = _context.Notifications.AsQueryable();

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var total = await data.CountAsync();

        if (total == 0)
        {
            return new NotificationPerformanceDto();
        }

        var sent = await data.CountAsync(x => x.Status == "Sent");
        var failed = await data.CountAsync(x => x.Status == "Failed");
        var pending = await data.CountAsync(x => x.Status == "Pending");

        var avgRetry = await data.AverageAsync(x => (double?)x.RetryCount) ?? 0;
        var maxRetry = await data.MaxAsync(x => (int?)x.RetryCount) ?? 0;

        return new NotificationPerformanceDto
        {
            Total = total,
            Sent = sent,
            Failed = failed,
            Pending = pending,

            SuccessRate = Math.Round((double)sent / total * 100, 2),
            FailureRate = Math.Round((double)failed / total * 100, 2),

            AvgRetryCount = Math.Round(avgRetry, 2),
            MaxRetryCount = maxRetry
        };
    }

    public async Task<WorkflowPerformanceDto> GetWorkflowPerformanceAsync(ReportQuery query)
    {
        _logger.LogInformation(
            "REPORT_WORKFLOW_PERFORMANCE | From: {From} | To: {To}",
            query.From?.ToString("yyyy-MM-dd") ?? "N/A",
            query.To?.ToString("yyyy-MM-dd") ?? "N/A");

        var data = _context.Workflows.AsQueryable();

        if (query.From.HasValue)
            data = data.Where(x => x.CreatedAt >= query.From.Value);

        if (query.To.HasValue)
            data = data.Where(x => x.CreatedAt <= query.To.Value);

        var total = await data.CountAsync();

        if (total == 0)
        {
            return new WorkflowPerformanceDto();
        }

        var completed = await data.CountAsync(x => x.Status == "Completed");
        var failed = await data.CountAsync(x => x.Status == "Failed");
        var inProgress = await data.CountAsync(x => x.Status == "InProgress");

        var avgRetry = await data.AverageAsync(x => (double?)x.RetryCount) ?? 0;
        var maxRetry = await data.MaxAsync(x => (int?)x.RetryCount) ?? 0;

        return new WorkflowPerformanceDto
        {
            Total = total,
            Completed = completed,
            Failed = failed,
            InProgress = inProgress,

            CompletionRate = Math.Round((double)completed / total * 100, 2),
            FailureRate = Math.Round((double)failed / total * 100, 2),

            AvgRetryCount = Math.Round(avgRetry, 2),
            MaxRetryCount = maxRetry
        };
    }
}
