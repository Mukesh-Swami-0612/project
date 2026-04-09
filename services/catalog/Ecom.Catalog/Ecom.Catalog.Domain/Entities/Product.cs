namespace Ecom.Catalog.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int StatusId { get; set; } = 1;
    public int CreatedBy { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public Category Category { get; set; } = null!;
    public Brand? Brand { get; set; }
    public ProductStatus Status { get; set; } = null!;
    public ICollection<ProductVariantCombination> Variants { get; set; } = new List<ProductVariantCombination>();
    public ICollection<MediaAsset> MediaAssets { get; set; } = new List<MediaAsset>();
}
