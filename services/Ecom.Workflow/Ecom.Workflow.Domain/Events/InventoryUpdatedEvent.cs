namespace Ecom.Workflow.Domain.Events;

public class InventoryUpdatedEvent
{
    public int ProductVariantId { get; set; }
    public int NewQuantity { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
