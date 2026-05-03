using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Rules;

namespace Ecom.Workflow.Application.Services;

public class PricingService : IPricingService
{
    private readonly IOutboxRepository _outbox;

    public PricingService(IOutboxRepository outbox) => _outbox = outbox;

    public async Task<PricingDto> SavePricingAsync(int productId, PricingDto dto)
    {
        if (!PriceValidationRule.IsSatisfiedBy(dto.SalePrice, dto.MRP))
            throw new InvalidOperationException("Sale price cannot exceed MRP.");

        // Persist price (repository call omitted for brevity)
        // Write outbox event for async propagation
        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "pricing.updated",
            Payload = System.Text.Json.JsonSerializer.Serialize(dto)
        });

        return dto;
    }

    public Task<PricingDto?> GetPricingAsync(int productVariantId) =>
        Task.FromResult<PricingDto?>(null);
}
