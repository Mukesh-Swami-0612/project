namespace Ecom.Catalog.Application.Exceptions;

/// <summary>
/// Exception thrown when a concurrency conflict is detected
/// Indicates that the entity was modified by another user
/// </summary>
public class ConcurrencyException : Exception
{
    public int EntityId { get; }
    public string EntityType { get; }

    public ConcurrencyException(string entityType, int entityId)
        : base($"{entityType} (ID: {entityId}) was modified by another user. Please refresh and try again.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }

    public ConcurrencyException(string entityType, int entityId, string message)
        : base(message)
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
