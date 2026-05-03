namespace Ecom.Workflow.Domain.Entities;

/// <summary>
/// Tracks processed events for idempotency (Phase 1: Tracking only, no blocking)
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "PROCESSED";
}
