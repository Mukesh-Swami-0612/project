namespace Ecom.Shared.Contracts.Interfaces;

/// <summary>
/// Interface for publishing events to message broker.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey = "") where T : class;
}
