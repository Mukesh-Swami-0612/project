namespace Ecom.Auth.Domain.Events;

public class UserCreatedEvent
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
