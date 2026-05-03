using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly CatalogDbContext _context;

    public AuditRepository(CatalogDbContext context) => _context = context;

    public async Task AddAsync(AuditLog auditLog)
    {
        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }
}
