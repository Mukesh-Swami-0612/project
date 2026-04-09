namespace Ecom.Catalog.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Deleted
    public string? Changes { get; set; } // JSON of changes
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
