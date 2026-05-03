using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Domain.Entities;
using Ecom.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecom.Workflow.Infrastructure.Services;

/// <summary>
/// Phase 1: Tracking only - does NOT block duplicate processing yet
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        WorkflowDbContext context,
        ILogger<IdempotencyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsProcessedAsync(Guid eventId)
    {
        try
        {
            // Handle empty GUID (events without EventId)
            if (eventId == Guid.Empty)
            {
                _logger.LogWarning("IsProcessedAsync called with empty EventId, returning false");
                return false;
            }

            var exists = await _context.IdempotencyRecords
                .AnyAsync(r => r.EventId == eventId);

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking idempotency for EventId {EventId}", eventId);
            // Phase 1: On error, return false to allow processing
            return false;
        }
    }

    public async Task MarkProcessedAsync(Guid eventId, string eventType)
    {
        try
        {
            // Handle empty GUID (events without EventId)
            if (eventId == Guid.Empty)
            {
                _logger.LogWarning("MarkProcessedAsync called with empty EventId, skipping");
                return;
            }

            var record = new IdempotencyRecord
            {
                EventId = eventId,
                EventType = eventType,
                ProcessedAt = DateTime.UtcNow,
                Status = "PROCESSED"
            };

            _context.IdempotencyRecords.Add(record);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Marked EventId {EventId} ({EventType}) as processed",
                eventId,
                eventType);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition: Another thread already inserted this EventId
            // This is safe to ignore - the record exists, which is what we want
            _logger.LogInformation(
                "Duplicate EventId {EventId} detected during insert (concurrent processing), safely ignored",
                eventId);
        }
        catch (Exception ex)
        {
            // Phase 1: Log error but don't throw - don't break message processing
            _logger.LogError(ex,
                "Error marking EventId {EventId} as processed, continuing anyway",
                eventId);
        }
    }

    private bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // SQL Server unique constraint violation error codes
        // 2601: Cannot insert duplicate key row
        // 2627: Violation of unique constraint
        var sqlException = ex.InnerException as Microsoft.Data.SqlClient.SqlException;
        return sqlException?.Number == 2601 || sqlException?.Number == 2627;
    }

    /// <summary>
    /// Phase 2: Execute business logic within a transaction that includes idempotency marking
    /// Ensures atomicity - either both succeed or both fail
    /// </summary>
    public async Task ExecuteWithIdempotencyAsync(Guid eventId, string eventType, Func<Task> businessLogic)
    {
        // Handle empty GUID
        if (eventId == Guid.Empty)
        {
            _logger.LogWarning("ExecuteWithIdempotencyAsync called with empty EventId, executing without transaction");
            await businessLogic();
            return;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Execute business logic first
            await businessLogic();

            // Mark as processed (within same transaction)
            var record = new IdempotencyRecord
            {
                EventId = eventId,
                EventType = eventType,
                ProcessedAt = DateTime.UtcNow,
                Status = "PROCESSED"
            };

            _context.IdempotencyRecords.Add(record);
            await _context.SaveChangesAsync();

            // Commit transaction - both operations succeed together
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Transactionally processed and marked EventId {EventId} ({EventType})",
                eventId,
                eventType);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Race condition during transaction - another thread already processed this
            await transaction.RollbackAsync();
            _logger.LogInformation(
                "Duplicate EventId {EventId} detected during transactional insert, rolled back",
                eventId);
            throw; // Let caller handle duplicate
        }
        catch (Exception ex)
        {
            // Any error - rollback everything
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Error during transactional processing of EventId {EventId}, rolled back",
                eventId);
            throw; // Propagate error for retry
        }
    }
}
