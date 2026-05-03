namespace Ecom.Workflow.Domain.Events;

public class PricingUpdatedEvent
{
    public int ProductVariantId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MRP { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
