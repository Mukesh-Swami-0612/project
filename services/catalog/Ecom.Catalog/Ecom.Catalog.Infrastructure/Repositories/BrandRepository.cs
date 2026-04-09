using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class BrandRepository : IBrandRepository
{
    private readonly CatalogDbContext _context;

    public BrandRepository(CatalogDbContext context) => _context = context;

    public async Task<IEnumerable<Brand>> GetAllAsync() =>
        await _context.Brands.Where(b => !b.IsDeleted).ToListAsync();
}
