namespace Ecom.Workflow.Application.Events;

/// <summary>
/// Event received when Catalog completes product validation
/// Response to ValidateProductCommand
/// </summary>
public class ProductValidatedEvent
{
    public Guid EventId { get; init; }
    public int ProductId { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
