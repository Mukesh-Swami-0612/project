namespace Ecom.Catalog.Domain.Entities;

// CQRS Read Model — updated via domain events from Workflow service
public class ProductReadModel
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}
