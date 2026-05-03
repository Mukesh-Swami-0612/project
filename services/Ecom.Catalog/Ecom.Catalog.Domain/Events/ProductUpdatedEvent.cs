namespace Ecom.Catalog.Domain.Events;

/// <summary>
/// Event raised when product is updated
/// Used to synchronize read model
/// </summary>
public class ProductUpdatedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
