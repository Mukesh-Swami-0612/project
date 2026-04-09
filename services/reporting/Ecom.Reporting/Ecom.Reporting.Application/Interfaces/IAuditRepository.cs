using Ecom.Reporting.Domain.Entities;

namespace Ecom.Reporting.Application.Interfaces;

public interface IAuditRepository
{
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityName, int entityId);
    Task<IEnumerable<AuditLog>> GetAllAsync(DateTime? from, DateTime? to);
    Task AddAsync(AuditLog log);
    Task<Dictionary<string, int>> GetActionCountsAsync();
    Task<IEnumerable<AuditLog>> GetFilteredAsync(DateTime? from, DateTime? to, string? reportType, int limit);
}
