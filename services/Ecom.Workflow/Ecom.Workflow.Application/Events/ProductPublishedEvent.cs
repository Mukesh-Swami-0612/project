namespace Ecom.Workflow.Application.Events;

/// <summary>
/// Event received when product is published in Catalog
/// Response to PublishProductCommand
/// </summary>
public class ProductPublishedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int PublishedBy { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
