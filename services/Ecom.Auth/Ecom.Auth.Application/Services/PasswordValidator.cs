using System.Text.RegularExpressions;
using Ecom.Auth.Application.Exceptions;

namespace Ecom.Auth.Application.Services;

/// <summary>
/// Validates password strength according to security requirements.
/// </summary>
public static class PasswordValidator
{
    private const int MinLength = 8;
    private const int MaxLength = 128;

    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ValidationException("Password is required.");

        if (password.Length < MinLength)
            throw new ValidationException($"Password must be at least {MinLength} characters long.");

        if (password.Length > MaxLength)
            throw new ValidationException($"Password must not exceed {MaxLength} characters.");

        // At least one uppercase letter
        if (!Regex.IsMatch(password, @"[A-Z]"))
            throw new ValidationException("Password must contain at least one uppercase letter.");

        // At least one lowercase letter
        if (!Regex.IsMatch(password, @"[a-z]"))
            throw new ValidationException("Password must contain at least one lowercase letter.");

        // At least one digit
        if (!Regex.IsMatch(password, @"\d"))
            throw new ValidationException("Password must contain at least one digit.");

        // At least one special character
        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]"))
            throw new ValidationException("Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>).");
    }

    public static bool IsStrong(string password)
    {
        try
        {
            Validate(password);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
