namespace Ecom.Workflow.Domain.Events;

public class ProductRejectedEvent
{
    public int ProductId { get; set; }
    public int RejectedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
