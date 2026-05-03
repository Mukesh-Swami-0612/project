namespace Ecom.Notification.Domain.Events;

/// <summary>
/// Event received when product is rejected
/// </summary>
public class ProductRejectedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public int RejectedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
