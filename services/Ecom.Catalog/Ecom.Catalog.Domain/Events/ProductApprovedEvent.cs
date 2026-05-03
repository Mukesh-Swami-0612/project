namespace Ecom.Catalog.Domain.Events;

/// <summary>
/// Event raised when product is approved by reviewer
/// Critical event for workflow progression and notifications
/// </summary>
public class ProductApprovedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ApprovedBy { get; set; }
    public string? Comments { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
