using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;
using Ecom.Catalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecom.Catalog.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly CatalogDbContext _context;

    public OutboxRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.OutboxMessages.AddAsync(message);
        // Note: SaveChanges is called by the caller within the same transaction
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize = 20)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task MarkAsProcessingAsync(Guid messageId)
    {
        var message = await _context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Processing;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAsProcessedAsync(Guid messageId)
    {
        var message = await _context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAsFailedAsync(Guid messageId, string error, int retryCount)
    {
        var message = await _context.OutboxMessages.FindAsync(messageId);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Failed;
            message.Error = error;
            message.RetryCount = retryCount;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<OutboxMessage>> GetRetryableMessagesAsync(int maxRetries = 3)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Failed && m.RetryCount < maxRetries)
            .OrderBy(m => m.OccurredOn)
            .Take(20)
            .ToListAsync();
    }

    public async Task DeleteOldProcessedMessagesAsync(DateTime olderThan)
    {
        var oldMessages = await _context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Processed && m.ProcessedOn < olderThan)
            .ToListAsync();

        _context.OutboxMessages.RemoveRange(oldMessages);
        await _context.SaveChangesAsync();
    }
}
