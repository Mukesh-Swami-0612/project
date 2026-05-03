using Asp.Versioning;
using Ecom.Workflow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Workflow.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/workflow/products/{productId}")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class PublishController : ControllerBase
{
    private readonly IPublishService _publishService;

    public PublishController(IPublishService publishService) => _publishService = publishService;

    [HttpGet("checklist")]
    public async Task<IActionResult> GetChecklist(int productId)
    {
        var checklist = await _publishService.GetChecklistAsync(productId);
        return Ok(checklist);
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish(int productId, [FromBody] int publishedBy)
    {
        await _publishService.PublishAsync(productId, publishedBy);
        return Ok();
    }

    [HttpPost("archive")]
    public async Task<IActionResult> Archive(int productId, [FromBody] int archivedBy)
    {
        await _publishService.ArchiveAsync(productId, archivedBy);
        return Ok();
    }
}
