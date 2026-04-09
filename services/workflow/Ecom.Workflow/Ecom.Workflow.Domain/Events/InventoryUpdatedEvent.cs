namespace Ecom.Workflow.Domain.Events;

public class InventoryUpdatedEvent
{
    public int ProductVariantId { get; set; }
    public int NewQuantity { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
