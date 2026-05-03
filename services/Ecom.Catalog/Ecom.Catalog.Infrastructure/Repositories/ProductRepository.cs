using Ecom.Catalog.Application.DTOs;
using Ecom.Catalog.Application.Exceptions;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context) => _context = context;

    public async Task<IEnumerable<Product>> GetAllAsync(string? search, int page = 1, int pageSize = 10)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Status)
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _context.Products
            .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Status)
            .Include(p => p.MediaAssets)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

    public Task<bool> SkuExistsAsync(string sku) =>
        _context.Products.AnyAsync(p => p.SKU == sku && !p.IsDeleted);

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // 🔥 CONCURRENCY CONFLICT: Product was modified by another user
            throw new ConcurrencyException("Product", product.Id);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            product.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    // ── QUERY ENGINE ──────────────────────────────────────────────────────────

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Query products with filtering, sorting, and pagination
    /// Uses composable IQueryable pipeline for efficient SQL generation
    /// </summary>
    public async Task<(List<Product> Items, int TotalCount)> QueryAsync(ProductQueryDto query)
    {
        // 🔥 START WITH BASE QUERY (AsNoTracking for read-only performance)
        var dbQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Status)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        // 🔥 FILTERING: Composable pipeline
        
        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.CategoryId == query.CategoryId.Value);
        }

        if (query.BrandId.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.BrandId == query.BrandId.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            dbQuery = dbQuery.Where(p => p.Status.Name == query.Status);
        }

        // Price filtering (when Price field is added to Product entity)
        // if (query.MinPrice.HasValue)
        // {
        //     dbQuery = dbQuery.Where(p => p.Price >= query.MinPrice.Value);
        // }
        //
        // if (query.MaxPrice.HasValue)
        // {
        //     dbQuery = dbQuery.Where(p => p.Price <= query.MaxPrice.Value);
        // }

        // 🔥 SEARCH: Name and SKU
        if (!string.IsNullOrEmpty(query.Search))
        {
            var searchLower = query.Search.ToLower();
            dbQuery = dbQuery.Where(p =>
                p.Name.ToLower().Contains(searchLower) ||
                p.SKU.ToLower().Contains(searchLower));
        }

        // 🔥 TOTAL COUNT: Before pagination (important for PagedResult)
        var totalCount = await dbQuery.CountAsync();

        // 🔥 SORTING: Pattern matching for clean code
        dbQuery = query.SortBy?.ToLower() switch
        {
            "name" => query.SortOrder == "asc"
                ? dbQuery.OrderBy(p => p.Name)
                : dbQuery.OrderByDescending(p => p.Name),

            "sku" => query.SortOrder == "asc"
                ? dbQuery.OrderBy(p => p.SKU)
                : dbQuery.OrderByDescending(p => p.SKU),

            "updatedat" => query.SortOrder == "asc"
                ? dbQuery.OrderBy(p => p.UpdatedAt)
                : dbQuery.OrderByDescending(p => p.UpdatedAt),

            // Default: sort by CreatedAt
            _ => query.SortOrder == "asc"
                ? dbQuery.OrderBy(p => p.CreatedAt)
                : dbQuery.OrderByDescending(p => p.CreatedAt)
        };

        // 🔥 PAGINATION: Skip and Take
        var items = await dbQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
