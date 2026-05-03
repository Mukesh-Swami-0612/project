using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Domain.Enums;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Repositories;

public class WorkflowInstanceRepository : IWorkflowInstanceRepository
{
    private readonly WorkflowDbContext _context;

    public WorkflowInstanceRepository(WorkflowDbContext context) => _context = context;

    public async Task<WorkflowInstance?> GetByIdAsync(Guid id) =>
        await _context.WorkflowInstances.FindAsync(id);

    public async Task<WorkflowInstance?> GetByProductIdAsync(int productId) =>
        await _context.WorkflowInstances
            .Where(w => w.ProductId == productId)
            .OrderByDescending(w => w.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<WorkflowInstance>> GetByStatusAsync(WorkflowStatus status) =>
        await _context.WorkflowInstances
            .Where(w => w.Status == status)
            .ToListAsync();

    public async Task<IEnumerable<WorkflowInstance>> GetFailedWithRetriesAsync(int maxRetries) =>
        await _context.WorkflowInstances
            .Where(w => w.Status == WorkflowStatus.Failed && w.RetryCount < maxRetries)
            .ToListAsync();

    public async Task<IEnumerable<WorkflowInstance>> GetDueRetriesAsync(DateTime currentTime) =>
        await _context.WorkflowInstances
            .Where(w => w.Status == WorkflowStatus.InProgress 
                && w.NextRetryAt != null 
                && w.NextRetryAt <= currentTime
                && w.RetryCount < w.MaxRetries)
            .ToListAsync();

    // 🔥 PRODUCTION: Get all failed workflows for saga recovery
    public async Task<IEnumerable<WorkflowInstance>> GetFailedWorkflowsAsync() =>
        await _context.WorkflowInstances
            .Where(w => w.Status == WorkflowStatus.Failed)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync();

    public async Task<PagedResult<WorkflowInstance>> QueryAsync(WorkflowQueryDto query)
    {
        var q = _context.WorkflowInstances.AsQueryable();

        // Filter by status
        if (!string.IsNullOrEmpty(query.Status))
        {
            if (Enum.TryParse<WorkflowStatus>(query.Status, true, out var status))
            {
                q = q.Where(x => x.Status == status);
            }
        }

        // Filter by step
        if (!string.IsNullOrEmpty(query.Step))
        {
            if (Enum.TryParse<WorkflowStep>(query.Step, true, out var step))
            {
                q = q.Where(x => x.CurrentStep == step);
            }
        }

        // Filter by product ID
        if (query.ProductId.HasValue)
        {
            q = q.Where(x => x.ProductId == query.ProductId.Value);
        }

        var totalCount = await q.CountAsync();

        var data = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<WorkflowInstance>
        {
            Data = data,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task AddAsync(WorkflowInstance instance)
    {
        await _context.WorkflowInstances.AddAsync(instance);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(WorkflowInstance instance)
    {
        instance.UpdatedAt = DateTime.UtcNow;
        _context.WorkflowInstances.Update(instance);
        await _context.SaveChangesAsync();
    }
}
