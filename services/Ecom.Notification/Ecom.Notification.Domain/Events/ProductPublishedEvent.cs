namespace Ecom.Notification.Domain.Events;

/// <summary>
/// Event received when product is published
/// </summary>
public class ProductPublishedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PublishedBy { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
