namespace Ecom.Catalog.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string entityName, int entityId, string action, string? changes, int userId, string userEmail, string? ipAddress);
}
