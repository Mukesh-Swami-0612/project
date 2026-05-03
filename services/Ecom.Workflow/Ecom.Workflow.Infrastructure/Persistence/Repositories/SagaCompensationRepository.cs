using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Persistence.Repositories;

public class SagaCompensationRepository : ISagaCompensationRepository
{
    private readonly WorkflowDbContext _context;

    public SagaCompensationRepository(WorkflowDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SagaCompensationLog log)
    {
        _context.SagaCompensationLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<SagaCompensationLog>> GetByWorkflowIdAsync(Guid workflowId)
    {
        return await _context.SagaCompensationLogs
            .Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.ExecutedAt)
            .ToListAsync();
    }

    public async Task<List<SagaCompensationLog>> GetByProductIdAsync(int productId)
    {
        return await _context.SagaCompensationLogs
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.ExecutedAt)
            .ToListAsync();
    }
}
