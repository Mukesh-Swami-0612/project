using Ecom.Reporting.Application.DTOs;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Reporting.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportExportService _exportService;

    public ReportsController(IReportExportService exportService) => _exportService = exportService;

    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ReportExportRequestDto request)
    {
        var fileBytes = await _exportService.ExportAsync(request);
        var fileName = $"report_{request.ReportType}_{DateTime.UtcNow:yyyyMMdd}.csv";
        return File(fileBytes, "text/csv", fileName);
    }
}
