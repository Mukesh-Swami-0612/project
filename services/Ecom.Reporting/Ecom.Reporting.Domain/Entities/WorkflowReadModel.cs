namespace Ecom.Reporting.Domain.Entities;

public class WorkflowReadModel
{
    public int Id { get; set; }
    public string WorkflowId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Completed, InProgress, Failed, Cancelled
    public string WorkflowType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
