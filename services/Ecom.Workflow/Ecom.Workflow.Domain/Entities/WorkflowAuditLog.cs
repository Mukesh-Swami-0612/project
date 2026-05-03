namespace Ecom.Workflow.Domain.Entities;

/// <summary>
/// Audit log for workflow operations
/// Stores readable business events with username, action, and timestamp
/// </summary>
public class WorkflowAuditLog
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid WorkflowId { get; set; }
    public int ProductId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; }
}
