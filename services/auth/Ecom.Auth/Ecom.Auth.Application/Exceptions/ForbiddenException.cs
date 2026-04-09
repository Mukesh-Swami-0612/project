namespace Ecom.Auth.Application.Exceptions;

/// <summary>
/// Exception thrown when a user lacks permission to access a resource.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(string message)
        : base(message, 403)
    {
    }

    public ForbiddenException()
        : base("You do not have permission to access this resource.", 403)
    {
    }
}
