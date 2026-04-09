namespace Ecom.Workflow.Domain.Events;

public class ProductApprovedEvent
{
    public int ProductId { get; set; }
    public int ApprovedBy { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
