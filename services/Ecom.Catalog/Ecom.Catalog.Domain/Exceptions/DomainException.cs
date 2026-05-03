namespace Ecom.Catalog.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain business rules are violated
/// Indicates invalid data or business logic violation
/// </summary>
public class DomainException : Exception
{
    public string? PropertyName { get; }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }
}
