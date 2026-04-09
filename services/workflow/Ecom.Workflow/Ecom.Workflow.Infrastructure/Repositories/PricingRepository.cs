using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Repositories;

public class PricingRepository
{
    private readonly WorkflowDbContext _context;

    public PricingRepository(WorkflowDbContext context) => _context = context;

    public Task<Price?> GetByVariantIdAsync(int variantId) =>
        _context.Prices.FirstOrDefaultAsync(p => p.ProductVariantId == variantId && p.IsActive);

    public async Task UpsertAsync(Price price)
    {
        var existing = await GetByVariantIdAsync(price.ProductVariantId);
        if (existing == null) await _context.Prices.AddAsync(price);
        else { existing.MRP = price.MRP; existing.SalePrice = price.SalePrice; existing.UpdatedAt = DateTime.UtcNow; }
        await _context.SaveChangesAsync();
    }
}
