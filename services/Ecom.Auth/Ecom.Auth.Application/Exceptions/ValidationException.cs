namespace Ecom.Auth.Application.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public class ValidationException : AppException
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message)
        : base(message, 400)
    {
        Errors = new[] { message };
    }

    public ValidationException(IEnumerable<string> errors)
        : base("Validation failed.", 400)
    {
        Errors = errors;
    }
}
