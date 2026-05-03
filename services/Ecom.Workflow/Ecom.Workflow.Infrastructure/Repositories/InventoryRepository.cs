using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Repositories;

public class InventoryRepository
{
    private readonly WorkflowDbContext _context;

    public InventoryRepository(WorkflowDbContext context) => _context = context;

    public Task<Inventory?> GetByVariantIdAsync(int variantId) =>
        _context.Inventories.FirstOrDefaultAsync(i => i.ProductVariantId == variantId);

    public async Task UpsertAsync(Inventory inventory)
    {
        var existing = await GetByVariantIdAsync(inventory.ProductVariantId);
        if (existing == null) await _context.Inventories.AddAsync(inventory);
        else { existing.Quantity = inventory.Quantity; existing.UpdatedAt = DateTime.UtcNow; }
        await _context.SaveChangesAsync();
    }
}
