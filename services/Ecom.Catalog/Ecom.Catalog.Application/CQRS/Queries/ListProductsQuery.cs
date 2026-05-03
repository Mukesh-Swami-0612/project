namespace Ecom.Catalog.Application.CQRS.Queries;

/// <summary>
/// Query to list products with filtering and pagination
/// Represents read operation in CQRS pattern
/// </summary>
public class ListProductsQuery
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? StatusId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}
