namespace Ecom.Notification.Domain.Entities;

/// <summary>
/// Tracks user login history for suspicious login detection
/// </summary>
public class UserLoginHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
