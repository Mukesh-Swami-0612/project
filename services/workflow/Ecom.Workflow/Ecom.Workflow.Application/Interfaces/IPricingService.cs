using Ecom.Workflow.Application.DTOs;

namespace Ecom.Workflow.Application.Interfaces;

public interface IPricingService
{
    Task<PricingDto> SavePricingAsync(int productId, PricingDto dto);
    Task<PricingDto?> GetPricingAsync(int productVariantId);
}
