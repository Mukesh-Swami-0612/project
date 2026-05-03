using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Interfaces;

/// <summary>
/// Service for handling workflow compensation (rollback) logic
/// </summary>
public interface IWorkflowCompensationService
{
    Task CompensatePublishingFailureAsync(WorkflowInstance workflow, string reason);
    Task CompensateApprovalFailureAsync(WorkflowInstance workflow, string reason);
}
