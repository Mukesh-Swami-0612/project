using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace Ecom.Workflow.Infrastructure.Services;

/// <summary>
/// Audit service for workflow operations
/// Logs business events with username, action, and timestamp to database
/// Provides readable audit trail for compliance and debugging
/// </summary>
public class WorkflowAuditService : IWorkflowAuditService
{
    private readonly WorkflowDbContext _context;
    private readonly IHttpContextAccessor _httpContext;

    public WorkflowAuditService(
        WorkflowDbContext context,
        IHttpContextAccessor httpContext)
    {
        _context = context;
        _httpContext = httpContext;
    }

    /// <summary>
    /// Log workflow action to audit table
    /// Extracts username from JWT claims (zero-trust architecture)
    /// </summary>
    public async Task LogAsync(
        string action,
        Guid workflowId,
        int productId,
        string message)
    {
        // ZERO TRUST: Extract username from JWT claims, not gateway headers
        var username = _httpContext.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? _httpContext.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? _httpContext.HttpContext?.User?.FindFirst("sub")?.Value
            ?? "System";

        var log = new WorkflowAuditLog
        {
            Username = username,
            Action = action,
            WorkflowId = workflowId,
            ProductId = productId,
            Message = message,
            LoggedAt = DateTime.UtcNow
        };

        _context.WorkflowAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
