namespace Ecom.Workflow.Domain.Events;

public class PricingUpdatedEvent
{
    public int ProductVariantId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal MRP { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
