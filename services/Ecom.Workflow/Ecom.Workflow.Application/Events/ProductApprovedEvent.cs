namespace Ecom.Workflow.Application.Events;

/// <summary>
/// Event received when product is approved in Catalog
/// Response to RequestApprovalCommand
/// </summary>
public class ProductApprovedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public int ApprovedBy { get; set; }
    public string? Comments { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
