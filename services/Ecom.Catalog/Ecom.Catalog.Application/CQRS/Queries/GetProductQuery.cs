namespace Ecom.Catalog.Application.CQRS.Queries;

/// <summary>
/// Query to get a single product by ID
/// Represents read operation in CQRS pattern
/// </summary>
public class GetProductQuery
{
    public int ProductId { get; set; }
}
