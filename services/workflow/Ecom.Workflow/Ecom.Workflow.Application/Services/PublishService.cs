using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Rules;

namespace Ecom.Workflow.Application.Services;

public class PublishService : IPublishService
{
    private readonly IOutboxRepository _outbox;

    public PublishService(IOutboxRepository outbox) => _outbox = outbox;

    public Task<PublishChecklistDto> GetChecklistAsync(int productId) =>
        Task.FromResult(new PublishChecklistDto());

    public async Task PublishAsync(int productId, int publishedBy)
    {
        var checklist = await GetChecklistAsync(productId);
        if (!PublishReadinessRule.IsSatisfiedBy(new PublishReadinessChecklist
        {
            HasMedia = checklist.HasMedia,
            HasPricing = checklist.HasPricing,
            HasInventory = checklist.HasInventory,
            HasDescription = checklist.HasDescription,
            IsApproved = checklist.IsApproved
        }))
            throw new InvalidOperationException("Product does not meet publish readiness criteria.");

        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "product.published",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { productId, publishedBy })
        });
    }

    public async Task ArchiveAsync(int productId, int archivedBy)
    {
        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "product.archived",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { productId, archivedBy })
        });
    }
}
