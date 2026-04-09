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
        await _context.SaveChangesAsync();
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
}
