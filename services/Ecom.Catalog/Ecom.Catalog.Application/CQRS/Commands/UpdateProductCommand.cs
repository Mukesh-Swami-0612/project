namespace Ecom.Catalog.Application.CQRS.Commands;

/// <summary>
/// Command to update an existing product
/// Represents write operation in CQRS pattern
/// </summary>
public class UpdateProductCommand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public string? RowVersion { get; set; } // Base64 encoded
}
