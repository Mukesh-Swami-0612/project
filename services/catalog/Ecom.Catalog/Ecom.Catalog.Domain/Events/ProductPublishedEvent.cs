namespace Ecom.Catalog.Domain.Events;

public class ProductPublishedEvent
{
    public int ProductId { get; set; }
    public int PublishedBy { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
