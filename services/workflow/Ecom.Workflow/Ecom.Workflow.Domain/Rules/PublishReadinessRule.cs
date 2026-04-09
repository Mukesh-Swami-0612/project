namespace Ecom.Workflow.Domain.Rules;

public class PublishReadinessChecklist
{
    public bool HasMedia { get; set; }
    public bool HasPricing { get; set; }
    public bool HasInventory { get; set; }
    public bool HasDescription { get; set; }
    public bool IsApproved { get; set; }
}

public static class PublishReadinessRule
{
    public static bool IsSatisfiedBy(PublishReadinessChecklist checklist) =>
        checklist.HasMedia &&
        checklist.HasPricing &&
        checklist.HasInventory &&
        checklist.HasDescription &&
        checklist.IsApproved;
}
