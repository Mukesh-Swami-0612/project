using Ecom.Reporting.Application.DTOs;

namespace Ecom.Reporting.Application.Interfaces;

public interface IReportingService
{
    Task<NotificationReportDto> GetNotificationReportAsync(ReportQuery query);
    Task<WorkflowReportDto> GetWorkflowReportAsync(ReportQuery query);
    Task<ProductReportDto> GetProductReportAsync(ReportQuery query);
    
    // Time-series trends
    Task<List<DailyNotificationTrendDto>> GetNotificationTrendsAsync(ReportQuery query);
    Task<List<DailyWorkflowTrendDto>> GetWorkflowTrendsAsync(ReportQuery query);
    Task<List<DailyProductTrendDto>> GetProductTrendsAsync(ReportQuery query);
    
    // Failure analysis
    Task<List<FailureAnalysisDto>> GetNotificationFailureAnalysisAsync(ReportQuery query);
    Task<List<FailureAnalysisDto>> GetWorkflowFailureAnalysisAsync(ReportQuery query);
    Task<List<RejectionAnalysisDto>> GetProductRejectionAnalysisAsync(ReportQuery query);
    
    // Hotspot detection
    Task<List<TopFailureEntityDto>> GetTopWorkflowFailuresAsync(ReportQuery query);
    Task<List<TopRejectedProductDto>> GetTopRejectedProductsAsync(ReportQuery query);
    
    // Performance metrics
    Task<NotificationPerformanceDto> GetNotificationPerformanceAsync(ReportQuery query);
    Task<WorkflowPerformanceDto> GetWorkflowPerformanceAsync(ReportQuery query);
}
