namespace Ecom.Catalog.Domain.Events;

public class ProductCreatedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
