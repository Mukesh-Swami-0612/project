namespace Ecom.Catalog.Application.DTOs;

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
}
