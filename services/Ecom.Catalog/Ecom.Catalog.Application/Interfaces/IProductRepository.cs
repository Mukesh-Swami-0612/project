using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(string? search, int page = 1, int pageSize = 10);
    Task<Product?> GetByIdAsync(int id);
    Task<bool> SkuExistsAsync(string sku);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    
    /// <summary>
    /// Query products with filtering, sorting, and pagination
    /// Returns items and total count for efficient pagination
    /// </summary>
    Task<(List<Product> Items, int TotalCount)> QueryAsync(ProductQueryDto query);
    
    /// <summary>
    /// Save changes to database
    /// </summary>
    Task SaveChangesAsync();
}
