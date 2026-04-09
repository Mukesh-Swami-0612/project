namespace Ecom.Auth.Application.Exceptions;

/// <summary>
/// Base exception for all application-specific exceptions.
/// Provides consistent error handling with HTTP status codes.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public AppException(string message, Exception innerException, int statusCode = 400)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
