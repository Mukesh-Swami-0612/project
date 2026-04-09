using Ecom.Workflow.Domain.Entities;

namespace Ecom.Workflow.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent);
    Task<IEnumerable<OutboxEvent>> GetUnprocessedAsync();
    Task MarkProcessedAsync(int id);
}
