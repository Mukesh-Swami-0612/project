namespace Ecom.Auth.Application.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
