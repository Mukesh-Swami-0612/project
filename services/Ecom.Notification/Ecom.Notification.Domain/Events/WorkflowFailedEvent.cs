namespace Ecom.Notification.Domain.Events;

/// <summary>
/// Event received when workflow execution fails
/// </summary>
public class WorkflowFailedEvent
{
    public Guid EventId { get; init; }
    public Guid WorkflowId { get; set; }
    public int ProductId { get; set; }
    public string Error { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
