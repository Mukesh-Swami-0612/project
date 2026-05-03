using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Interfaces;

public interface IWorkflowAuditRepository
{
    Task<PagedResult<WorkflowAuditLog>> GetLogsAsync(Guid workflowId, WorkflowLogQueryDto query);
    Task<List<WorkflowAuditLog>> GetAllLogsAsync(Guid workflowId, WorkflowLogQueryDto query);
}
