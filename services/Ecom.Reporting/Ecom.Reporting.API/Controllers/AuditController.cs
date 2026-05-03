using Asp.Versioning;
using Ecom.Reporting.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Reporting.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/audit")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService) => _auditService = auditService;

    [HttpGet("products/{productId}")]
    public async Task<IActionResult> GetProductAudit(int productId)
    {
        var logs = await _auditService.GetByProductIdAsync(productId);
        return Ok(logs);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var logs = await _auditService.GetAllAsync(from, to);
        return Ok(logs);
    }
}
