using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Domain.Entities;

namespace Ecom.Auth.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task LogAsync(int? userId, string email, string action, string status, string ip)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Email = email,
            Action = action,
            Status = status,
            IpAddress = ip
        };

        await _auditLogRepository.AddAsync(log);
    }

    public async Task<(List<AuditLogDto> logs, int total)> GetLogsAsync(AuditLogFilterDto filter)
    {
        return await _auditLogRepository.GetLogsAsync(filter);
    }
}
