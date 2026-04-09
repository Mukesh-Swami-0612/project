namespace Ecom.Workflow.Domain.Entities;

public class OutboxEvent
{
    public int Id { get; set; }
    public string? EventKey { get; set; }
    public string? EventType { get; set; }
    public string? Payload { get; set; }
    public bool IsProcessed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
