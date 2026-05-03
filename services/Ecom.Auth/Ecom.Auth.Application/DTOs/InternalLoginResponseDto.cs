namespace Ecom.Auth.Application.DTOs;

/// <summary>
/// Internal response containing tokens for cookie setting
/// This is used internally between AuthService and AuthController
/// </summary>
public class InternalLoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Role { get; set; } = string.Empty;
    public string RedirectTo { get; set; } = string.Empty;
}