using Ecom.Workflow.Application.DTOs;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Repositories;

public class WorkflowAuditRepository : IWorkflowAuditRepository
{
    private readonly WorkflowDbContext _context;

    public WorkflowAuditRepository(WorkflowDbContext context) => _context = context;

    public async Task<PagedResult<WorkflowAuditLog>> GetLogsAsync(
        Guid workflowId,
        WorkflowLogQueryDto query)
    {
        var q = BuildQuery(workflowId, query);

        var total = await q.CountAsync();

        var data = await q
            .OrderByDescending(x => x.LoggedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<WorkflowAuditLog>
        {
            Data = data,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<List<WorkflowAuditLog>> GetAllLogsAsync(
        Guid workflowId,
        WorkflowLogQueryDto query)
    {
        var q = BuildQuery(workflowId, query);

        return await q
            .OrderByDescending(x => x.LoggedAt)
            .ToListAsync();
    }

    private IQueryable<WorkflowAuditLog> BuildQuery(Guid workflowId, WorkflowLogQueryDto query)
    {
        var q = _context.WorkflowAuditLogs
            .Where(x => x.WorkflowId == workflowId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(query.Action))
            q = q.Where(x => x.Action == query.Action);

        if (!string.IsNullOrEmpty(query.Username))
            q = q.Where(x => x.Username.Contains(query.Username));

        // Date range filtering
        if (query.FromDate.HasValue)
            q = q.Where(x => x.LoggedAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
        {
            // Include full day: 2026-04-20 means up to 2026-04-20 23:59:59.9999999
            var toDate = query.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            q = q.Where(x => x.LoggedAt <= toDate);
        }

        return q;
    }
}
