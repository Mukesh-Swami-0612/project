namespace Ecom.Notification.Domain.Events;

/// <summary>
/// Event received when a user successfully logs in
/// Includes device and location information for suspicious login detection
/// </summary>
public class UserLoginSuccessEvent
{
    public Guid EventId { get; init; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    
    // 🔍 DISTRIBUTED TRACING: Optional trace context
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
}
