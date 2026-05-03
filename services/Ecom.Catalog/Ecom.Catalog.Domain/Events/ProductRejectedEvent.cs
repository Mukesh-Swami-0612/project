namespace Ecom.Catalog.Domain.Events;

/// <summary>
/// Event raised when product is rejected by reviewer
/// Critical event for workflow updates and notifications
/// </summary>
public class ProductRejectedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RejectedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
