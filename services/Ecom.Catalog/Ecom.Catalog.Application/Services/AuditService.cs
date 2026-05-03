using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Ecom.Catalog.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditRepository auditRepository, ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository;
        _logger = logger;
    }

    public async Task LogAsync(string entityName, int entityId, string action, string? changes, int userId, string userEmail, string? ipAddress)
    {
        try
        {
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                Changes = changes,
                UserId = userId,
                UserEmail = userEmail,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(auditLog);

            // Structured logging for better readability
            _logger.LogInformation(
                "AUDIT_LOG | Action: {Action} | Entity: {EntityName} | EntityId: {EntityId} | User: {UserEmail} | UserId: {UserId} | IP: {IpAddress} | Time: {Time}",
                action, entityName, entityId, userEmail, userId, ipAddress ?? "N/A", auditLog.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "AUDIT_LOG_FAILED | Entity: {EntityName} | EntityId: {EntityId} | Action: {Action} | Error: {Error}",
                entityName, entityId, action, ex.Message);
        }
    }
}
