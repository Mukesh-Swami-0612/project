namespace Ecom.Workflow.Application.Interfaces;

/// <summary>
/// Interface for workflow audit logging
/// </summary>
public interface IWorkflowAuditService
{
    Task LogAsync(string action, Guid workflowId, int productId, string message);
}
