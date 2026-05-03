using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class MediaRepository : IMediaRepository
{
    private readonly CatalogDbContext _context;

    public MediaRepository(CatalogDbContext context) => _context = context;

    public async Task<IEnumerable<MediaAsset>> GetByProductIdAsync(int productId) =>
        await _context.MediaAssets.Where(m => m.ProductId == productId && !m.IsDeleted).ToListAsync();

    public async Task AddAsync(MediaAsset asset)
    {
        await _context.MediaAssets.AddAsync(asset);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var asset = await _context.MediaAssets.FindAsync(id);
        if (asset != null) { asset.IsDeleted = true; await _context.SaveChangesAsync(); }
    }
}
