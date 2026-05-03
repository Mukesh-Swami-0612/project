using Ecom.Auth.Application.DTOs;

namespace Ecom.Auth.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(int? userId, string email, string action, string status, string ip);
    Task<(List<AuditLogDto> logs, int total)> GetLogsAsync(AuditLogFilterDto filter);
}
