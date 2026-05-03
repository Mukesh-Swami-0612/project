namespace Ecom.Auth.Application.DTOs;

public class SessionDto
{
    public int Id { get; set; }
    public string DeviceInfo { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsRevoked { get; set; }
}
