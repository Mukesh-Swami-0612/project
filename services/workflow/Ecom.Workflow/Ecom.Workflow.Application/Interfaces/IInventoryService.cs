using Ecom.Workflow.Application.DTOs;

namespace Ecom.Workflow.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryDto> SaveInventoryAsync(int productId, InventoryDto dto);
    Task<InventoryDto?> GetInventoryAsync(int productVariantId);
}
