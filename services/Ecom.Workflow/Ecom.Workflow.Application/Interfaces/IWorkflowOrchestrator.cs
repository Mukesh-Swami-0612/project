namespace Ecom.Workflow.Application.Interfaces;

/// <summary>
/// Core orchestration engine interface
/// Processes workflow instances and drives state transitions
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Process a workflow instance and determine next action
    /// </summary>
    Task ProcessAsync(Guid workflowId);
}
