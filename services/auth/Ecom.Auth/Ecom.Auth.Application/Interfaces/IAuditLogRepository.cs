using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Domain.Entities;

namespace Ecom.Auth.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog);
    Task<(List<AuditLogDto> logs, int total)> GetLogsAsync(AuditLogFilterDto filter);
}
