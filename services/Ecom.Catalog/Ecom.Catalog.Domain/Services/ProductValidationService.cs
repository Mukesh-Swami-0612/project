using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Domain.Exceptions;

namespace Ecom.Catalog.Domain.Services;

/// <summary>
/// Domain service for validating product business rules
/// Centralizes all validation logic to ensure data correctness
/// </summary>
public class ProductValidationService
{
    /// <summary>
    /// Validates product data for creation
    /// </summary>
    public void ValidateForCreation(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new DomainException("Product name is required", nameof(product.Name));

        if (product.Name.Length > 150)
            throw new DomainException("Product name cannot exceed 150 characters", nameof(product.Name));

        if (string.IsNullOrWhiteSpace(product.SKU))
            throw new DomainException("SKU is required", nameof(product.SKU));

        if (product.SKU.Length > 100)
            throw new DomainException("SKU cannot exceed 100 characters", nameof(product.SKU));

        ValidateSKUFormat(product.SKU);

        if (product.CategoryId <= 0)
            throw new DomainException("Valid category is required", nameof(product.CategoryId));
    }

    /// <summary>
    /// Validates product data for update
    /// </summary>
    public void ValidateForUpdate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            throw new DomainException("Product name cannot be empty", nameof(product.Name));

        if (product.Name.Length > 150)
            throw new DomainException("Product name cannot exceed 150 characters", nameof(product.Name));

        if (string.IsNullOrWhiteSpace(product.SKU))
            throw new DomainException("SKU cannot be empty", nameof(product.SKU));

        if (product.SKU.Length > 100)
            throw new DomainException("SKU cannot exceed 100 characters", nameof(product.SKU));

        ValidateSKUFormat(product.SKU);

        if (product.CategoryId <= 0)
            throw new DomainException("Valid category is required", nameof(product.CategoryId));
    }

    /// <summary>
    /// Validates product is ready for review submission
    /// </summary>
    public void ValidateForReview(Product product)
    {
        if (!product.HasMinimumData())
            throw new DomainException("Product does not meet minimum requirements for review");

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new DomainException("Product name is required for review", nameof(product.Name));

        if (string.IsNullOrWhiteSpace(product.SKU))
            throw new DomainException("SKU is required for review", nameof(product.SKU));

        if (product.CategoryId <= 0)
            throw new DomainException("Valid category is required for review", nameof(product.CategoryId));
    }

    /// <summary>
    /// Validates product is ready for publishing
    /// This is the most critical validation - ensures product is complete
    /// </summary>
    public void ValidateForPublish(Product product)
    {
        // 🔥 CRITICAL: Product must have all required data before going live

        if (string.IsNullOrWhiteSpace(product.Name))
            throw new DomainException("Product name is required for publishing", nameof(product.Name));

        if (string.IsNullOrWhiteSpace(product.SKU))
            throw new DomainException("SKU is required for publishing", nameof(product.SKU));

        if (product.CategoryId <= 0)
            throw new DomainException("Valid category is required for publishing", nameof(product.CategoryId));

        // 🔥 MEDIA VALIDATION: At least one image required
        if (product.MediaAssets == null || !product.MediaAssets.Any())
            throw new DomainException("At least one image is required for publishing");

        // Check for primary image
        if (!product.MediaAssets.Any(m => m.IsPrimary))
            throw new DomainException("A primary image must be set for publishing");

        // 🔥 SKU VALIDATION: Must be unique and valid format
        ValidateSKUFormat(product.SKU);

        // 🔥 CATEGORY VALIDATION: Category must be active (if we track that)
        // This would require loading the category, so we'll do it in the service layer
    }

    /// <summary>
    /// Validates SKU format
    /// SKU should follow a consistent pattern for business operations
    /// </summary>
    private void ValidateSKUFormat(string sku)
    {
        // 🔥 BUSINESS RULE: SKU format validation
        // Example: Must be alphanumeric with hyphens, no spaces
        if (sku.Contains(' '))
            throw new DomainException("SKU cannot contain spaces", "SKU");

        // Must contain at least one alphanumeric character
        if (!sku.Any(char.IsLetterOrDigit))
            throw new DomainException("SKU must contain at least one alphanumeric character", "SKU");

        // Check for invalid characters (only allow letters, numbers, hyphens, underscores)
        if (!sku.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            throw new DomainException("SKU can only contain letters, numbers, hyphens, and underscores", "SKU");
    }

    /// <summary>
    /// Validates that product can be archived
    /// </summary>
    public void ValidateForArchive(Product product)
    {
        // Business rule: Can archive from any state except Draft
        // (Draft products should be deleted instead)
        if (product.GetLifecycleStatus() == Enums.ProductLifecycleStatus.Draft)
            throw new DomainException("Draft products should be deleted, not archived");
    }
}
