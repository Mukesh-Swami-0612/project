namespace Ecom.Workflow.Domain.Enums;

public enum WorkflowStep
{
    Created,
    
    ValidationPending,
    ValidationCompleted,
    
    ApprovalPending,
    Approved,
    Rejected,
    
    Publishing,
    Published,
    
    Completed,
    Failed
}
