namespace Ecom.Workflow.Domain.Enums;

public enum WorkflowStatus
{
    Draft = 1,
    InEnrichment = 2,
    ReadyForReview = 3,
    Approved = 4,
    Published = 5,
    Rejected = 6,
    Archived = 7
}
