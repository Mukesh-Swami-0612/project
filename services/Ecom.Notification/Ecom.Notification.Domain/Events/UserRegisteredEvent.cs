namespace Ecom.Notification.Domain.Events;

/// <summary>
/// Event received when a new user registers
/// </summary>
public class UserRegisteredEvent
{
    public Guid EventId { get; init; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
