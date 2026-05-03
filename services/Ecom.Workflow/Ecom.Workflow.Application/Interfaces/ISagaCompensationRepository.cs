using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Interfaces;

/// <summary>
/// Repository for saga compensation logs
/// </summary>
public interface ISagaCompensationRepository
{
    Task AddAsync(SagaCompensationLog log);
    Task<List<SagaCompensationLog>> GetByWorkflowIdAsync(Guid workflowId);
    Task<List<SagaCompensationLog>> GetByProductIdAsync(int productId);
}
