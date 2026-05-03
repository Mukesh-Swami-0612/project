using Asp.Versioning;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Reporting.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/reports/dashboard")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _dashboardService.GetSummaryAsync();
        return Ok(summary);
    }
}
