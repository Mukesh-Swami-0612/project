namespace Ecom.Workflow.Application.DTOs;

public class PricingDto
{
    public int ProductVariantId { get; set; }
    public decimal MRP { get; set; }
    public decimal SalePrice { get; set; }
}
