namespace Ecom.Notification.Domain.Events;

/// <summary>
/// Event received when product is approved
/// </summary>
public class ProductApprovedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public int ApprovedBy { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
