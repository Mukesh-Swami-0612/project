using Ecom.Workflow.Domain.Enums;

namespace Ecom.Workflow.Domain.Entities;

public class WorkflowInstance
{
    public Guid Id { get; set; }
    
    public int ProductId { get; set; }
    
    public WorkflowStatus Status { get; set; }
    
    public WorkflowStep CurrentStep { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public int MaxRetries { get; set; } = 3;
    
    public DateTime? NextRetryAt { get; set; }
    
    public string? LastError { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public string? CorrelationId { get; set; }
}
