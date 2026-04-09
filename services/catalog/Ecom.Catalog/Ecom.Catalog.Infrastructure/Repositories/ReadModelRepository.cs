using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class ReadModelRepository : IReadModelRepository
{
    private readonly CatalogDbContext _context;

    public ReadModelRepository(CatalogDbContext context) => _context = context;

    public async Task<IEnumerable<ProductReadModel>> GetAllAsync() =>
        await _context.ProductReadModels.AsNoTracking().ToListAsync();

    public Task<ProductReadModel?> GetByIdAsync(int id) =>
        _context.ProductReadModels.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task UpsertAsync(ProductReadModel model)
    {
        var existing = await _context.ProductReadModels.FirstOrDefaultAsync(p => p.Id == model.Id);
        if (existing == null)
            await _context.ProductReadModels.AddAsync(model);
        else
            _context.Entry(existing).CurrentValues.SetValues(model);

        await _context.SaveChangesAsync();
    }
}
