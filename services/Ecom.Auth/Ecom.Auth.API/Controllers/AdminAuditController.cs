using Asp.Versioning;
using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

namespace Ecom.Auth.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/audit-logs")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
[EnableRateLimiting("admin")]
public class AdminAuditController : ControllerBase
{
    private readonly IAuditService _auditService;
    private readonly AuditExportService _auditExportService;

    public AdminAuditController(IAuditService auditService, AuditExportService auditExportService)
    {
        _auditService = auditService;
        _auditExportService = auditExportService;
    }

    /// <summary>
    /// Get audit logs with filters and pagination (Admin only)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] AuditLogFilterDto filter)
    {
        // Log admin access for security tracking
        Log.Information(
            "Admin viewed audit logs | Email={Email} | Filters: Email={FilterEmail}, Action={FilterAction}, FromDate={FromDate}, ToDate={ToDate}",
            User.Identity?.Name ?? "Unknown",
            filter.Email,
            filter.Action,
            filter.FromDate,
            filter.ToDate
        );

        var (logs, total) = await _auditService.GetLogsAsync(filter);

        return Ok(new
        {
            Total = total,
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            Data = logs
        });
    }

    /// <summary>
    /// Export audit logs to Excel file (Admin only)
    /// </summary>
    /// <param name="filter">Filter criteria for audit logs</param>
    /// <returns>Excel file with filtered audit logs</returns>
    /// <response code="200">Excel file generated successfully</response>
    /// <response code="400">Export limit exceeded</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <remarks>
    /// Maximum 10,000 records can be exported at once to prevent memory issues.
    /// Use filters to narrow down the results if limit is exceeded.
    /// </remarks>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> ExportLogs([FromQuery] AuditLogFilterDto filter)
    {
        var adminEmail = User.Identity?.Name ?? "Unknown";

        // Log admin export action for security tracking
        Log.Information(
            "Admin initiated audit log export | Email={Email} | Filters: Email={FilterEmail}, Action={FilterAction}, FromDate={FromDate}, ToDate={ToDate}",
            adminEmail,
            filter.Email,
            filter.Action,
            filter.FromDate,
            filter.ToDate
        );

        // Get all logs without pagination for export
        var exportFilter = new AuditLogFilterDto
        {
            Email = filter.Email,
            Action = filter.Action,
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            PageNumber = 1,
            PageSize = int.MaxValue // Get all records for export
        };

        var (logs, total) = await _auditService.GetLogsAsync(exportFilter);

        // Check if export limit would be exceeded
        if (total > 10000)
        {
            Log.Warning(
                "Admin export limit exceeded | Email={Email} | TotalRecords={Total}",
                adminEmail,
                total
            );

            return BadRequest(new
            {
                Status = 400,
                Message = $"Export limit exceeded. Found {total} records, maximum 10,000 allowed. Please use filters to narrow down results."
            });
        }

        try
        {
            var excelBytes = _auditExportService.GenerateExcel(logs, filter);

            var fileName = $"AuditLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            Log.Information(
                "Admin export completed successfully | Email={Email} | RecordsExported={Count} | FileName={FileName}",
                adminEmail,
                logs.Count(),
                fileName
            );

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(
                ex,
                "Admin export failed | Email={Email} | Error={Error}",
                adminEmail,
                ex.Message
            );

            return BadRequest(new
            {
                Status = 400,
                Message = ex.Message
            });
        }
    }
}
