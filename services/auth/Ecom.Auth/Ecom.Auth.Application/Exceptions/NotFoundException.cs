namespace Ecom.Auth.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, 404)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.", 404)
    {
    }
}
