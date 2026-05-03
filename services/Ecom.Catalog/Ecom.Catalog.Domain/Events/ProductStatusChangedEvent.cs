namespace Ecom.Catalog.Domain.Events;

/// <summary>
/// Event raised when product lifecycle status changes
/// </summary>
public class ProductStatusChangedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int ChangedBy { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
