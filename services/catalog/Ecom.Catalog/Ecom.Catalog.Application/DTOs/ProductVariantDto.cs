namespace Ecom.Catalog.Application.DTOs;

public class ProductVariantDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string SKU { get; set; } = string.Empty;
}
