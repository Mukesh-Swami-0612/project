using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Interfaces;

/// <summary>
/// Repository for read-optimized product queries
/// Reads from denormalized ProductReadModel table
/// </summary>
public interface IReadModelRepository
{
    Task<ProductReadModel?> GetByIdAsync(int id);
    
    Task<List<ProductReadModel>> GetAllAsync();
    
    Task<(List<ProductReadModel> Items, int TotalCount)> QueryAsync(
        string? search = null,
        int? categoryId = null,
        int? brandId = null,
        int? statusId = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDescending = false);
    
    Task SyncFromProductAsync(int productId);
    Task DeleteAsync(int productId);
}
