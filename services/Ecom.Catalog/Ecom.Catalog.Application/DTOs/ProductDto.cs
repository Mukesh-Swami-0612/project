namespace Ecom.Catalog.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; } // Base64 encoded for client
}
