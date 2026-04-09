using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Services;

public class ApprovalService : IApprovalService
{
    private readonly IOutboxRepository _outbox;

    public ApprovalService(IOutboxRepository outbox) => _outbox = outbox;

    public async Task SubmitForReviewAsync(int productId, int submittedBy)
    {
        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "product.submitted",
            Payload = System.Text.Json.JsonSerializer.Serialize(new { productId, submittedBy })
        });
    }

    public async Task ApproveAsync(ApprovalDto dto)
    {
        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "product.approved",
            Payload = System.Text.Json.JsonSerializer.Serialize(dto)
        });
    }

    public async Task RejectAsync(ApprovalDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Comments))
            throw new InvalidOperationException("Rejection reason is required.");

        await _outbox.AddAsync(new OutboxEvent
        {
            EventKey = Guid.NewGuid().ToString(),
            EventType = "product.rejected",
            Payload = System.Text.Json.JsonSerializer.Serialize(dto)
        });
    }
}
