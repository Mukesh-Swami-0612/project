using Ecom.Workflow.Domain.Enums;

namespace Ecom.Workflow.Domain.Entities;

/// <summary>
/// Tracks compensation actions executed during saga rollback
/// Provides audit trail for distributed transaction rollback
/// </summary>
public class SagaCompensationLog
{
    public Guid Id { get; set; }
    
    public Guid WorkflowId { get; set; }
    
    public int ProductId { get; set; }
    
    public WorkflowStep FailedAtStep { get; set; }
    
    public SagaCompensationAction CompensationAction { get; set; }
    
    public string? CompensationDetails { get; set; }
    
    public bool CompensationSuccessful { get; set; }
    
    public string? CompensationError { get; set; }
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    public string? CorrelationId { get; set; }
    
    // 🔥 PRODUCTION: Retry tracking
    public int RetryCount { get; set; } = 0;
    
    public int MaxRetries { get; set; } = 3;
    
    public DateTime? NextRetryAt { get; set; }
    
    // 🔥 PRODUCTION: Compensation status
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Failed
}
