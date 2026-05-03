using Asp.Versioning;
using Ecom.Catalog.Application.Common;
using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Catalog.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/products")]
[ApiVersion("1.0")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService) => _productService = productService;

    /// <summary>
    /// Query products with structured parameters (NEW - Recommended)
    /// </summary>
    [HttpGet("query")]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<ProductDto>>> QueryProducts([FromQuery] ProductQueryDto query)
    {
        var result = await _productService.GetProductsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all products (Legacy - kept for backward compatibility)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var products = await _productService.GetAllAsync(search, page, pageSize);
        return Ok(products);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ProductManager")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        var result = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ProductManager,ContentExecutive")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
    {
        var result = await _productService.UpdateAsync(id, dto);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteAsync(id);
        return NoContent();
    }

    // ── LIFECYCLE MANAGEMENT ─────────────────────────────────────────────────────

    /// <summary>
    /// Get product status and valid transitions
    /// </summary>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ProductStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductStatusDto>> GetStatus(int id)
    {
        var status = await _productService.GetProductStatusAsync(id);
        return Ok(status);
    }

    /// <summary>
    /// Transition product to new status
    /// </summary>
    [HttpPost("{id}/transition")]
    [Authorize(Roles = "Admin,ProductManager")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> TransitionStatus(int id, [FromBody] TransitionProductStatusDto dto)
    {
        var result = await _productService.TransitionStatusAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Submit product for review
    /// </summary>
    [HttpPost("{id}/submit-for-review")]
    [Authorize(Roles = "Admin,ProductManager,ContentExecutive")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> SubmitForReview(int id)
    {
        var result = await _productService.SubmitForReviewAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Approve product
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Approve(int id)
    {
        var result = await _productService.ApproveAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Reject product with reason
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Reject(int id, [FromBody] string reason)
    {
        var result = await _productService.RejectAsync(id, reason);
        return Ok(result);
    }

    /// <summary>
    /// Publish product to storefront
    /// </summary>
    [HttpPost("{id}/publish")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Publish(int id)
    {
        var result = await _productService.PublishAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Archive product
    /// </summary>
    [HttpPost("{id}/archive")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> Archive(int id)
    {
        var result = await _productService.ArchiveAsync(id);
        return Ok(result);
    }
}
