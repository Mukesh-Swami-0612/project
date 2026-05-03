namespace Ecom.Catalog.Application.DTOs;

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? RowVersion { get; set; } // Base64 encoded from client
}
