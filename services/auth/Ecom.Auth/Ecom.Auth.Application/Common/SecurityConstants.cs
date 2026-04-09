namespace Ecom.Auth.Application.Common;

/// <summary>
/// Security-related constants for the Auth service.
/// </summary>
public static class SecurityConstants
{
    // Account Lockout
    public const int MaxFailedLoginAttempts = 5;
    public const int LockoutDurationMinutes = 15;

    // Password Requirements
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    // Token Expiry
    public const int EmailVerificationTokenExpiryHours = 1;
    public const int PasswordResetTokenExpiryHours = 1;

    // Rate Limiting (requests per minute)
    public const int LoginRateLimit = 5;
    public const int ForgotPasswordRateLimit = 3;
    public const int RefreshTokenRateLimit = 10;

    // JWT
    public const int MinJwtKeyLength = 32;
}
