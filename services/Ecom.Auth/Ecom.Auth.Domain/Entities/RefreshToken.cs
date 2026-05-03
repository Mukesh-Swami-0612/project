namespace Ecom.Auth.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty; // 🔥 SECURITY: Store hash, not plain token
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; } // 🔥 SECURITY: Store hash of replacement token
    
    // 🔥 SESSION MANAGEMENT: Device tracking
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    
    // 🔥 RACE CONDITION PROTECTION: Optimistic concurrency
    public byte[] RowVersion { get; set; } = null!;
    
    public User User { get; set; } = null!;
}
