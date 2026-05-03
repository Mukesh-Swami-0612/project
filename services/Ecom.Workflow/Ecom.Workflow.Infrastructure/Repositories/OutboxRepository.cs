using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Workflow.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly WorkflowDbContext _context;

    public OutboxRepository(WorkflowDbContext context) => _context = context;

    public async Task AddAsync(OutboxEvent outboxEvent)
    {
        await _context.OutboxEvents.AddAsync(outboxEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OutboxEvent>> GetUnprocessedAsync() =>
        await _context.OutboxEvents.Where(e => !e.IsProcessed).ToListAsync();

    public async Task MarkProcessedAsync(int id)
    {
        var ev = await _context.OutboxEvents.FindAsync(id);
        if (ev != null) { ev.IsProcessed = true; await _context.SaveChangesAsync(); }
    }
}
