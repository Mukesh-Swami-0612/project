using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

/// <summary>
/// Repository for read-optimized queries
/// Reads from denormalized ProductReadModel table
/// </summary>
public class ReadModelRepository : IReadModelRepository
{
    private readonly CatalogDbContext _context;

    public ReadModelRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<ProductReadModel?> GetByIdAsync(int id)
    {
        return await _context.ProductReadModels
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<ProductReadModel>> GetAllAsync()
    {
        return await _context.ProductReadModels.ToListAsync();
    }

    public async Task<(List<ProductReadModel> Items, int TotalCount)> QueryAsync(
        string? search = null,
        int? categoryId = null,
        int? brandId = null,
        int? statusId = null,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = _context.ProductReadModels.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => 
                p.ProductName.Contains(search) || 
                p.CategoryName.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryName == categoryId.ToString());
        }

        if (!string.IsNullOrWhiteSpace(statusId?.ToString()))
        {
            query = query.Where(p => p.Status == statusId.ToString());
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "name" => sortDescending 
                ? query.OrderByDescending(p => p.ProductName)
                : query.OrderBy(p => p.ProductName),
            "price" => sortDescending
                ? query.OrderByDescending(p => p.Price)
                : query.OrderBy(p => p.Price),
            "stock" => sortDescending
                ? query.OrderByDescending(p => p.Stock)
                : query.OrderBy(p => p.Stock),
            _ => query.OrderBy(p => p.Id)
        };

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task SyncFromProductAsync(int productId)
    {
        // Get product from write model
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Status)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            return;

        // Find or create read model
        var readModel = await _context.ProductReadModels
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (readModel == null)
        {
            readModel = new ProductReadModel { Id = productId };
            _context.ProductReadModels.Add(readModel);
        }

        // Sync data from write model to read model
        readModel.ProductName = product.Name;
        readModel.CategoryName = product.Category?.Name ?? string.Empty;
        readModel.Price = 0; // TODO: Get from variants
        readModel.Stock = 0; // TODO: Get from inventory
        readModel.Status = product.Status?.Name ?? string.Empty;
        readModel.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int productId)
    {
        var readModel = await _context.ProductReadModels
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (readModel != null)
        {
            _context.ProductReadModels.Remove(readModel);
            await _context.SaveChangesAsync();
        }
    }
}
