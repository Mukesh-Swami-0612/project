namespace Ecom.Workflow.Domain.Events;

/// <summary>
/// Event raised when workflow completes successfully
/// Critical event for reporting and audit trail
/// </summary>
public class WorkflowCompletedEvent
{
    public Guid WorkflowId { get; set; }
    public int ProductId { get; set; }
    public string WorkflowType { get; set; } = "ProductWorkflow";
    public int EntityId { get; set; }
    public int RetryCount { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
