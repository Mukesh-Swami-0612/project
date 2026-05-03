using Ecom.Catalog.Domain.Enums;

namespace Ecom.Catalog.Domain.Entities;

/// <summary>
/// Lookup table for product lifecycle statuses
/// </summary>
public class ProductStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Helper to convert to enum
    public ProductLifecycleStatus ToEnum() => (ProductLifecycleStatus)Id;
}
