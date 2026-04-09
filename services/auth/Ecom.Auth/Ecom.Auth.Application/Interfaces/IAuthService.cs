using Ecom.Auth.Application.DTOs;

namespace Ecom.Auth.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    Task<(UserDto user, string verificationToken)> SignupAsync(SignupRequestDto request);
    Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken, int userId);
    Task LogoutAllAsync(int userId);

    // Password reset flow
    Task<string?> ForgotPasswordAsync(string email);
    Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
    Task<bool> VerifyEmailAsync(string token);
    
    // New methods
    Task<string?> ResendVerificationEmailAsync(string email);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}
