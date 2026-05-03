namespace Ecom.Workflow.Domain.Entities;

public class InventoryLog
{
    public int Id { get; set; }
    public int VariantId { get; set; }
    public int ChangeQty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
