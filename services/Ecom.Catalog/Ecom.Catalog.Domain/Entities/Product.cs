using Ecom.Catalog.Domain.Enums;

namespace Ecom.Catalog.Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int StatusId { get; set; } = 1; // Default: Draft
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

    // 🔥 DOMAIN METHODS: Encapsulate business logic

    /// <summary>
    /// Gets current lifecycle status as enum
    /// </summary>
    public ProductLifecycleStatus GetLifecycleStatus() => (ProductLifecycleStatus)StatusId;

    /// <summary>
    /// Checks if product is in editable state
    /// </summary>
    public bool IsEditable()
    {
        var status = GetLifecycleStatus();
        return status == ProductLifecycleStatus.Draft ||
               status == ProductLifecycleStatus.InEnrichment ||
               status == ProductLifecycleStatus.Rejected;
    }

    /// <summary>
    /// Checks if product can be deleted
    /// </summary>
    public bool IsDeletable()
    {
        var status = GetLifecycleStatus();
        return status == ProductLifecycleStatus.Draft ||
               status == ProductLifecycleStatus.Rejected ||
               status == ProductLifecycleStatus.Archived;
    }

    /// <summary>
    /// Checks if product is published and visible to customers
    /// </summary>
    public bool IsPublished() => GetLifecycleStatus() == ProductLifecycleStatus.Published;

    /// <summary>
    /// Validates product has minimum required data
    /// </summary>
    public bool HasMinimumData()
    {
        return !string.IsNullOrWhiteSpace(Name) &&
               !string.IsNullOrWhiteSpace(SKU) &&
               CategoryId > 0;
    }
}
