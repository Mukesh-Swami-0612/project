using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Repositories;

public class ApprovalRepository
{
    private readonly WorkflowDbContext _context;

    public ApprovalRepository(WorkflowDbContext context) => _context = context;

    public async Task<IEnumerable<Approval>> GetByProductIdAsync(int productId) =>
        await _context.Approvals.Where(a => a.ProductId == productId && !a.IsDeleted).ToListAsync();

    public async Task AddAsync(Approval approval)
    {
        await _context.Approvals.AddAsync(approval);
        await _context.SaveChangesAsync();
    }
}
