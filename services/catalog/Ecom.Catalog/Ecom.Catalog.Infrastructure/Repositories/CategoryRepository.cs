using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly CatalogDbContext _context;

    public CategoryRepository(CatalogDbContext context) => _context = context;

    public async Task<IEnumerable<Category>> GetAllAsync() =>
        await _context.Categories.Where(c => !c.IsDeleted).ToListAsync();

    public Task<Category?> GetByIdAsync(int id) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();
    }
}
