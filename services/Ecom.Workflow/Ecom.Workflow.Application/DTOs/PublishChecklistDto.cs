namespace Ecom.Workflow.Application.DTOs;

public class PublishChecklistDto
{
    public bool HasMedia { get; set; }
    public bool HasPricing { get; set; }
    public bool HasInventory { get; set; }
    public bool HasDescription { get; set; }
    public bool IsApproved { get; set; }
    public bool IsReadyToPublish => HasMedia && HasPricing && HasInventory && HasDescription && IsApproved;
}
