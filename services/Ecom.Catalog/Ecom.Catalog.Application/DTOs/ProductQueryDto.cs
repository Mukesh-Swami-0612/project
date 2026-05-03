namespace Ecom.Catalog.Application.DTOs;

/// <summary>
/// Structured query parameters for product search and filtering
/// Provides consistent API contract for querying products
/// </summary>
public class ProductQueryDto
{
    // ── PAGINATION ────────────────────────────────────────────────────────────
    
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;
    
    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    public int PageSize { get; set; } = 10;

    // ── FILTERING ─────────────────────────────────────────────────────────────
    
    /// <summary>
    /// Filter by category ID
    /// </summary>
    public int? CategoryId { get; set; }
    
    /// <summary>
    /// Filter by brand ID
    /// </summary>
    public int? BrandId { get; set; }
    
    /// <summary>
    /// Filter by minimum price
    /// </summary>
    public decimal? MinPrice { get; set; }
    
    /// <summary>
    /// Filter by maximum price
    /// </summary>
    public decimal? MaxPrice { get; set; }
    
    /// <summary>
    /// Filter by lifecycle status (Draft, Published, etc.)
    /// </summary>
    public string? Status { get; set; }

    // ── SEARCH ────────────────────────────────────────────────────────────────
    
    /// <summary>
    /// Search term for name and SKU
    /// </summary>
    public string? Search { get; set; }

    // ── SORTING ───────────────────────────────────────────────────────────────
    
    /// <summary>
    /// Sort field (name, sku, createdAt, updatedAt)
    /// </summary>
    public string? SortBy { get; set; } = "createdAt";
    
    /// <summary>
    /// Sort order (asc, desc)
    /// </summary>
    public string? SortOrder { get; set; } = "desc";

    // ── VALIDATION ────────────────────────────────────────────────────────────
    
    /// <summary>
    /// Validates query parameters
    /// </summary>
    public void Validate()
    {
        // Ensure page is at least 1
        if (Page < 1)
            Page = 1;

        // Limit page size to prevent abuse
        if (PageSize < 1)
            PageSize = 10;
        if (PageSize > 100)
            PageSize = 100;

        // Normalize sort order
        if (!string.IsNullOrEmpty(SortOrder))
        {
            SortOrder = SortOrder.ToLower();
            if (SortOrder != "asc" && SortOrder != "desc")
                SortOrder = "desc";
        }

        // Normalize sort field
        if (!string.IsNullOrEmpty(SortBy))
        {
            SortBy = SortBy.ToLower();
            var validSortFields = new[] { "name", "sku", "createdat", "updatedat" };
            if (!validSortFields.Contains(SortBy))
                SortBy = "createdat";
        }

        // Validate price range
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
        {
            // Swap if min > max
            (MinPrice, MaxPrice) = (MaxPrice, MinPrice);
        }
    }
}
