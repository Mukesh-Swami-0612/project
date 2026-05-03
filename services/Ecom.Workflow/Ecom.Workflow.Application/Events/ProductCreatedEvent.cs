namespace Ecom.Workflow.Application.Events;

/// <summary>
/// Event received when a product is created in Catalog service
/// Must match Catalog.Domain.Events.ProductCreatedEvent structure
/// </summary>
public class ProductCreatedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
