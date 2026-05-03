using Asp.Versioning;
using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Reporting.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/reports")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportExportService _exportService;
    private readonly IReportingService _reportingService;

    public ReportsController(
        IReportExportService exportService,
        IReportingService reportingService)
    {
        _exportService = exportService;
        _reportingService = reportingService;
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotificationReport([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetNotificationReportAsync(query);
        return Ok(result);
    }

    [HttpGet("notifications/trends")]
    public async Task<IActionResult> GetNotificationTrends([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetNotificationTrendsAsync(query);
        return Ok(result);
    }

    [HttpGet("workflows")]
    public async Task<IActionResult> GetWorkflowReport([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetWorkflowReportAsync(query);
        return Ok(result);
    }

    [HttpGet("workflows/trends")]
    public async Task<IActionResult> GetWorkflowTrends([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetWorkflowTrendsAsync(query);
        return Ok(result);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProductReport([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetProductReportAsync(query);
        return Ok(result);
    }

    [HttpGet("products/trends")]
    public async Task<IActionResult> GetProductTrends([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetProductTrendsAsync(query);
        return Ok(result);
    }

    [HttpGet("notifications/failures")]
    public async Task<IActionResult> GetNotificationFailures([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetNotificationFailureAnalysisAsync(query);
        return Ok(result);
    }

    [HttpGet("workflows/failures")]
    public async Task<IActionResult> GetWorkflowFailures([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetWorkflowFailureAnalysisAsync(query);
        return Ok(result);
    }

    [HttpGet("products/rejections")]
    public async Task<IActionResult> GetProductRejections([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetProductRejectionAnalysisAsync(query);
        return Ok(result);
    }

    [HttpGet("workflows/top-failures")]
    public async Task<IActionResult> GetTopWorkflowFailures([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetTopWorkflowFailuresAsync(query);
        return Ok(result);
    }

    [HttpGet("products/top-rejections")]
    public async Task<IActionResult> GetTopRejectedProducts([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetTopRejectedProductsAsync(query);
        return Ok(result);
    }

    [HttpGet("notifications/performance")]
    public async Task<IActionResult> GetNotificationPerformance([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetNotificationPerformanceAsync(query);
        return Ok(result);
    }

    [HttpGet("workflows/performance")]
    public async Task<IActionResult> GetWorkflowPerformance([FromQuery] ReportQuery query)
    {
        var result = await _reportingService.GetWorkflowPerformanceAsync(query);
        return Ok(result);
    }

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ReportExportRequestDto request)
    {
        var fileBytes = await _exportService.ExportAsync(request);
        var fileName = $"report_{request.ReportType}_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(fileBytes, "text/csv", fileName);
    }
}
