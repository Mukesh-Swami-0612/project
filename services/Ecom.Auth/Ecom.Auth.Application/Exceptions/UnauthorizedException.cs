namespace Ecom.Auth.Application.Exceptions;

/// <summary>
/// Exception thrown when authentication fails or credentials are invalid.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(message, 401)
    {
    }

    public UnauthorizedException()
        : base("Authentication failed. Invalid credentials.", 401)
    {
    }
}
