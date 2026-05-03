namespace Ecom.Catalog.Domain.Events;

/// <summary>
/// Event raised when product validation completes
/// Critical event for workflow progression
/// </summary>
public class ProductValidatedEvent
{
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string? ValidationMessage { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
