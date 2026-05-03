using Asp.Versioning;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Catalog.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/products/{productId}/media")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin,ProductManager,ContentExecutive")]
public class MediaController : ControllerBase
{
    private readonly MediaService _mediaService;

    public MediaController(MediaService mediaService) => _mediaService = mediaService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MediaAssetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<MediaAssetDto>>> GetMedia(int productId)
    {
        var media = await _mediaService.GetByProductIdAsync(productId);
        return Ok(media);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(int productId, [FromBody] CreateMediaAssetDto dto)
    {
        await _mediaService.AddMediaAsync(productId, dto.FileUrl, dto.FileType, dto.IsPrimary, dto.AltText);
        return Ok();
    }
}
