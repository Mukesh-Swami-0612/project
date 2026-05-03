namespace Ecom.Catalog.Application.CQRS.Commands;

/// <summary>
/// Command to create a new product
/// Represents write operation in CQRS pattern
/// </summary>
public class CreateProductCommand
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int CreatedBy { get; set; }
}
