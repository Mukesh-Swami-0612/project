using Asp.Versioning;
using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Workflow.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/workflow/products/{productId}/pricing")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin,ProductManager")]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricingService;

    public PricingController(IPricingService pricingService) => _pricingService = pricingService;

    [HttpPut]
    public async Task<IActionResult> SavePricing(int productId, [FromBody] PricingDto dto)
    {
        var result = await _pricingService.SavePricingAsync(productId, dto);
        return Ok(result);
    }
}
