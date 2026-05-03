using Asp.Versioning;
using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Workflow.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/workflow/products/{productId}")]
[ApiVersion("1.0")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;

    public ApprovalController(IApprovalService approvalService) => _approvalService = approvalService;

    [HttpPost("submit")]
    [Authorize(Roles = "Admin,ProductManager")]
    public async Task<IActionResult> Submit(int productId, [FromBody] int submittedBy)
    {
        await _approvalService.SubmitForReviewAsync(productId, submittedBy);
        return Ok();
    }

    [HttpPut("approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int productId, [FromBody] ApprovalDto dto)
    {
        await _approvalService.ApproveAsync(dto);
        return Ok();
    }

    [HttpPut("reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int productId, [FromBody] ApprovalDto dto)
    {
        await _approvalService.RejectAsync(dto);
        return Ok();
    }
}
