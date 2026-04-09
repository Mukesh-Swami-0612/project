namespace Ecom.Workflow.Domain.Entities;

public class Price
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public decimal MRP { get; set; }
    public decimal SalePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string? EventKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }
}
