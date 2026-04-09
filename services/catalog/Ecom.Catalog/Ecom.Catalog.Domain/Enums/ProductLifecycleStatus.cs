namespace Ecom.Catalog.Domain.Enums;

public enum ProductLifecycleStatus
{
    Draft = 1,
    InEnrichment = 2,
    ReadyForReview = 3,
    Approved = 4,
    Published = 5,
    Rejected = 6,
    Archived = 7
}
