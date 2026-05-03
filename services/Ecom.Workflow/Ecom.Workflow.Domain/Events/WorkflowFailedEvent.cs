namespace Ecom.Workflow.Domain.Events;

/// <summary>
/// Event raised when workflow execution fails
/// Critical event for notifications and audit trail
/// </summary>
public class WorkflowFailedEvent
{
    public Guid WorkflowId { get; set; }
    public int ProductId { get; set; }
    public string Error { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
