namespace Ecom.Catalog.Domain.Events;

/// <summary>
/// Event raised when product is published to storefront
/// Critical event for notifications, search indexing, and reporting
/// </summary>
public class ProductPublishedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int PublishedBy { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
