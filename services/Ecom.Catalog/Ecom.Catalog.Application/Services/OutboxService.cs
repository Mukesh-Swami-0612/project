using System.Text.Json;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Domain.Entities;

namespace Ecom.Catalog.Application.Services;

/// <summary>
/// Service for adding events to the outbox
/// Events are saved in the same transaction as domain changes
/// </summary>
public class OutboxService
{
    private readonly IOutboxRepository _outboxRepository;

    public OutboxService(IOutboxRepository outboxRepository)
    {
        _outboxRepository = outboxRepository;
    }

    /// <summary>
    /// Add event to outbox (will be published by background worker)
    /// </summary>
    public async Task AddEventAsync<TEvent>(TEvent @event) where TEvent : class
    {
        var eventType = @event.GetType().Name;
        var payload = JsonSerializer.Serialize(@event);

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Payload = payload,
            OccurredOn = DateTime.UtcNow,
            Status = OutboxMessageStatus.Pending,
            RetryCount = 0
        };

        await _outboxRepository.AddAsync(outboxMessage);
        // Note: SaveChanges is called by ProductService within the same transaction
    }
}
