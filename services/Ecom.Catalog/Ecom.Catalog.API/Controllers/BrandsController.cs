using Asp.Versioning;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Catalog.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/brands")]
[ApiVersion("1.0")]
[Authorize]
public class BrandsController : ControllerBase
{
    private readonly BrandService _brandService;

    public BrandsController(BrandService brandService) => _brandService = brandService;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BrandDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<BrandDto>>> GetAll()
    {
        var brands = await _brandService.GetAllAsync();
        return Ok(brands);
    }
}
