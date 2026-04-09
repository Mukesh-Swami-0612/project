namespace Ecom.Auth.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public string Email { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;   // LOGIN, SIGNUP, etc.
    public string Status { get; set; } = string.Empty;   // SUCCESS / FAILED

    public string IpAddress { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
