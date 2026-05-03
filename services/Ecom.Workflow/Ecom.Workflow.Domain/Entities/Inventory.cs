namespace Ecom.Workflow.Domain.Entities;

public class Inventory
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
