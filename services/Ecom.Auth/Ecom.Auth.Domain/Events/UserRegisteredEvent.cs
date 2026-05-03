namespace Ecom.Auth.Domain.Events;

/// <summary>
/// Event published when a new user successfully registers
/// </summary>
public class UserRegisteredEvent
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
