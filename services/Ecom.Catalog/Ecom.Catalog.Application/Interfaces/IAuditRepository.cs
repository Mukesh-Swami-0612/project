using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

public interface IAuditRepository
{
    Task AddAsync(AuditLog auditLog);
}
