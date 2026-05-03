namespace Ecom.Auth.Application.Exceptions;

/// <summary>
/// Exception thrown when a resource conflict occurs (e.g., duplicate email).
/// Maps to HTTP 409 Conflict.
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(string message)
        : base(message, 409)
    {
    }
}
