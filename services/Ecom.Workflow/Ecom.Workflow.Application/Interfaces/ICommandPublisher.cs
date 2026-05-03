namespace Ecom.Workflow.Application.Interfaces;

/// <summary>
/// Publishes commands to other services via message broker
/// Commands are INSTRUCTIONS, not events
/// </summary>
public interface ICommandPublisher
{
    /// <summary>
    /// Publish a command to a specific routing key
    /// </summary>
    Task PublishAsync<T>(string routingKey, T command) where T : class;
}
