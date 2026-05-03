namespace Ecom.Catalog.Application.Common;

/// <summary>
/// Standard paginated response wrapper
/// Provides consistent structure for all paginated API responses
/// </summary>
/// <typeparam name="T">Type of items in the result set</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// List of items for current page
    /// </summary>
    public List<T> Data { get; set; } = new();

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a paged result
    /// </summary>
    public static PagedResult<T> Create(List<T> data, int page, int pageSize, int totalCount)
    {
        return new PagedResult<T>
        {
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}
