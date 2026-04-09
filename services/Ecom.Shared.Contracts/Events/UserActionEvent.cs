namespace Ecom.Shared.Contracts.Events;

/// <summary>
/// Event published when a user performs an action in the Auth service.
/// Consumed by Reporting service for audit logging.
/// </summary>
public class UserActionEvent
{
    public string EntityName { get; set; } = "User";
    public int? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Email { get; set; }
    public string? AdditionalInfo { get; set; }
}
