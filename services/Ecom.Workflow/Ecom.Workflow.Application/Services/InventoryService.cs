using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IOutboxRepository _outbox;

    public InventoryService(IOutboxRepository outbox) => _outbox = outbox;

    public async Task<InventoryDto> SaveInventoryAsync(int productId, InventoryDto dto)
    {
        if (dto.Quantity < 0)
            throw new InvalidOperationException("Quantity cannot be negative.");

        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "inventory.updated",
            Payload = System.Text.Json.JsonSerializer.Serialize(dto)
        });

        return dto;
    }

    public Task<InventoryDto?> GetInventoryAsync(int productVariantId) =>
        Task.FromResult<InventoryDto?>(null);
}
