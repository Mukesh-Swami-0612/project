using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Workflow.API.Controllers;

[ApiController]
[Route("api/workflow/products/{productId}/inventory")]
[Authorize(Roles = "Admin,ProductManager")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService) => _inventoryService = inventoryService;

    [HttpPut]
    public async Task<IActionResult> SaveInventory(int productId, [FromBody] InventoryDto dto)
    {
        var result = await _inventoryService.SaveInventoryAsync(productId, dto);
        return Ok(result);
    }
}
