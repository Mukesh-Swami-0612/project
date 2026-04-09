using Ecom.Reporting.Application.DTOs;

namespace Ecom.Reporting.Application.Interfaces;

public interface IAuditService
{
    Task<IEnumerable<AuditLogDto>> GetByProductIdAsync(int productId);
    Task<IEnumerable<AuditLogDto>> GetAllAsync(DateTime? from, DateTime? to);
    Task WriteAsync(AuditLogDto dto);
}
