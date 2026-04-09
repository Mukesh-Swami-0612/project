namespace Ecom.Catalog.Application.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int CreatedBy { get; set; }
}
