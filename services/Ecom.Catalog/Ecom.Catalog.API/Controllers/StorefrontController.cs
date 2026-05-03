using Asp.Versioning;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Catalog.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/storefront")]
[ApiVersion("1.0")]
public class StorefrontController : ControllerBase
{
    private readonly StorefrontService _storefrontService;

    public StorefrontController(StorefrontService storefrontService) => _storefrontService = storefrontService;

    [HttpGet("products")]
    [ProducesResponseType(typeof(IEnumerable<ProductReadModelDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductReadModelDto>>> GetPublishedProducts()
    {
        var products = await _storefrontService.GetPublishedProductsAsync();
        return Ok(products);
    }

    [HttpGet("products/{id}")]
    [ProducesResponseType(typeof(ProductReadModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductReadModelDto>> GetProductPreview(int id)
    {
        var product = await _storefrontService.GetProductPreviewAsync(id);
        return product == null ? NotFound() : Ok(product);
    }
}
