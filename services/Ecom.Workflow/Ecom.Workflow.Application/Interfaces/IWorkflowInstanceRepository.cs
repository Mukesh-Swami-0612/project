using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;

namespace Ecom.Workflow.Application.Interfaces;

public interface IWorkflowInstanceRepository
{
    Task<WorkflowInstance?> GetByIdAsync(Guid id);
    Task<WorkflowInstance?> GetByProductIdAsync(int productId);
    Task<IEnumerable<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status);
    Task<IEnumerable<WorkflowInstance>> GetFailedWithRetriesAsync(int maxRetries);
    Task<IEnumerable<WorkflowInstance>> GetDueRetriesAsync(DateTime currentTime);
    Task<IEnumerable<WorkflowInstance>> GetFailedWorkflowsAsync(); // 🔥 PRODUCTION: For saga recovery
    Task<PagedResult<WorkflowInstance>> QueryAsync(WorkflowQueryDto query);
    Task AddAsync(WorkflowInstance instance);
    Task UpdateAsync(WorkflowInstance instance);
}
