using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Exceptions;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Domain.Entities;
using Ecom.Shared.Contracts.Events;
using Ecom.Shared.Contracts.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Ecom.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _config;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEmailService _emailService;
    private readonly IHostEnvironment _env;

    public AuthService(
        IUserRepository userRepository, 
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        ILogger<AuthService> logger, 
        IConfiguration config,
        IEventPublisher eventPublisher,
        IHttpContextAccessor httpContextAccessor,
        IEmailService emailService,
        IHostEnvironment env)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _logger = logger;
        _config = config;
        _eventPublisher = eventPublisher;
        _httpContextAccessor = httpContextAccessor;
        _emailService = emailService;
        _env = env;
    }

    private string GetCorrelationId()
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
        
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            _logger.LogWarning("CorrelationId not found in HttpContext, generated new one: {CorrelationId}", correlationId);
        }
        
        return correlationId;
    }

    private static string GenerateOtp(int digits = 6)
    {
        var upperBound = (int)Math.Pow(10, digits);
        var value = RandomNumberGenerator.GetInt32(0, upperBound);
        return value.ToString(new string('0', digits));
    }

    // ── SIGNUP ───────────────────────────────────────────────────────────────────
    public async Task<(UserDto user, string verificationToken)> SignupAsync(SignupRequestDto request)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new ConflictException("Email already registered.");

        // Validate role exists in database
        var roleExists = await _userRepository.RoleExistsAsync(request.RoleId);
        if (!roleExists)
            throw new ValidationException("Invalid role specified.");

        // Validate password strength
        PasswordValidator.Validate(request.Password);

        // Use transaction to ensure atomicity of user + verification token creation
        using var transaction = await _userRepository.BeginTransactionAsync();
        
        try
        {
            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = request.RoleId,
                IsEmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            // Stage inserts without immediate SaveChanges to avoid partial commits
            await _userRepository.AddAsync(user, saveChanges: false);

            // Generate and stage email verification token
            var verificationToken = new EmailVerificationToken
            {
                User = user,
                Token = GenerateOtp(),
                ExpiresAt = DateTime.UtcNow.AddHours(SecurityConstants.EmailVerificationTokenExpiryHours),
                IsUsed = false
            };

            await _userRepository.AddEmailVerificationTokenAsync(verificationToken, saveChanges: false);

            // Persist both entities in a single SaveChanges and commit
            await _userRepository.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send verification email (after transaction commit)
            try
            {
                await _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationToken.Token);
                _logger.LogInformation("Verification email sent to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
                _logger.LogWarning("Continuing without email delivery; user can trigger resend verification.");
            }

            _logger.LogInformation(
                "Action={Action} UserId={UserId} Email={Email} Status={Status}",
                "Signup", user.Id, user.Email, "SUCCESS");

            // Publish event for audit logging
            await _eventPublisher.PublishAsync(new UserActionEvent
            {
                EntityId = user.Id,
                Action = "Signup",
                EventType = "SUCCESS",
                Email = user.Email,
                CorrelationId = GetCorrelationId()
            });

            var userDto = new UserDto { Id = user.Id, Name = user.Name, Email = user.Email };
            return (userDto, verificationToken.Token);
        }
        catch
        {
            // Rollback transaction on any error (ignore if already completed)
            try { await transaction.RollbackAsync(); } catch (InvalidOperationException) { }
            throw;
        }
    }

    // ── LOGIN ────────────────────────────────────────────────────────────────────
    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        // Fake hash for timing attack prevention
        var fakeHash = "$2a$11$abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQR";

        // User not found or soft-deleted → perform fake verification to prevent timing attacks
        if (user == null || user.IsDeleted)
        {
            // Always perform hash verification to maintain consistent response time
            BCrypt.Net.BCrypt.Verify(request.Password, fakeHash);

            _logger.LogWarning(
                "Action={Action} Email={Email} Status={Status} Reason={Reason}",
                "Login", request.Email, "FAIL", "UserNotFound");
            throw new UnauthorizedException("Invalid credentials.");
        }

        // Inactive account
        if (!user.IsActive)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "Login", user.Id, user.Email, "FAIL", "AccountDisabled");
            throw new UnauthorizedException("Account is disabled. Please contact support.");
        }

        // Email not verified
        if (!user.IsEmailVerified)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "Login", user.Id, user.Email, "FAIL", "EmailNotVerified");
            throw new UnauthorizedException("Please verify your email before logging in.");
        }

        // Lockout check
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason} LockoutEnd={LockoutEnd}",
                "Login", user.Id, user.Email, "FAIL", "AccountLocked", user.LockoutEnd);
            throw new UnauthorizedException("Account is locked out due to too many failed attempts. Try again later.");
        }

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= SecurityConstants.MaxFailedLoginAttempts)
                user.LockoutEnd = DateTime.UtcNow.AddMinutes(SecurityConstants.LockoutDurationMinutes);

            await _userRepository.UpdateAsync(user);

            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason} FailedAttempts={FailedAttempts}",
                "Login", user.Id, user.Email, "FAIL", "InvalidPassword", user.FailedLoginAttempts);

            throw new UnauthorizedException("Invalid credentials.");
        }

        // Successful login → reset lockout state
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        await _userRepository.UpdateAsync(user);

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.SaveRefreshTokenAsync(user.Id, refreshToken);

        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "15");

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Email={Email} Role={Role} Status={Status}",
            "Login", user.Id, user.Email, user.Role?.RoleName, "SUCCESS");

        // Publish event for audit logging
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = user.Id,
            Action = "Login",
            EventType = "SUCCESS",
            Email = user.Email,
            AdditionalInfo = $"Role: {user.Role?.RoleName}",
            CorrelationId = GetCorrelationId()
        });

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Role = user.Role?.RoleName ?? string.Empty,
            RedirectTo = GetRedirectPath(user.Role?.RoleName ?? string.Empty)
        };
    }

    // ── Role-Based Navigation Mapping ─────────────────────────────────────────────
    private static string GetRedirectPath(string role)
    {
        return role switch
        {
            Roles.Admin => "/admin/dashboard",
            Roles.ProductManager => "/products/dashboard",
            Roles.ContentExecutive => "/content/dashboard",
            Roles.Customer => "/catalog/browse",
            _ => "/dashboard"
        };
    }

    // ── REFRESH TOKEN ────────────────────────────────────────────────────────────
    public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ValidationException("Refresh token is required.");

        var result = await _tokenService.RotateRefreshTokenAsync(refreshToken);

        if (result == null)
        {
            _logger.LogWarning(
                "Action={Action} Status={Status} Reason={Reason}",
                "RefreshToken", "FAIL", "InvalidOrExpiredToken");
            throw new UnauthorizedException("Invalid, expired, or reused refresh token. Please log in again.");
        }

        var expiryMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "15");

        // Extract role from new access token for redirect path
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Value.newAccessToken);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role);
        var role = roleClaim?.Value ?? string.Empty;

        _logger.LogInformation(
            "Action={Action} Status={Status}",
            "RefreshToken", "SUCCESS");

        return new LoginResponseDto
        {
            AccessToken = result.Value.newAccessToken,
            RefreshToken = result.Value.newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Role = role,
            RedirectTo = GetRedirectPath(role)
        };
    }

    // ── REVOKE TOKEN ─────────────────────────────────────────────────────────────
    public async Task RevokeTokenAsync(string refreshToken)
    {
        // Revoke is handled by the controller which knows the userId from the JWT claim
        // This overload is for cases where we only have the token string (e.g. from body)
        // The controller-level logout uses ITokenService.RevokeTokenAsync(token, userId) directly
        await Task.CompletedTask;
    }

    // ── FORGOT PASSWORD ───────────────────────────────────────────────────────────
    public async Task<string?> ForgotPasswordAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning(
                "Action={Action} Email={Email} Status={Status} Reason={Reason}",
                "ForgotPassword", email, "FAIL", "UserNotFound");
            return null; // Silent — prevent email enumeration
        }

        // Generate password reset token
        var resetToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = GenerateOtp(),
            ExpiresAt = DateTime.UtcNow.AddHours(SecurityConstants.PasswordResetTokenExpiryHours),
            IsUsed = false
        };

        await _userRepository.AddEmailVerificationTokenAsync(resetToken);

        // Send password reset email
        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetToken.Token);
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            _logger.LogWarning("Continuing without sending password reset email; user will need to retry.");
        }

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Email={Email} Status={Status}",
            "ForgotPassword", user.Id, email, "SUCCESS");

        return resetToken.Token;
    }

    // ── RESET PASSWORD ────────────────────────────────────────────────────────────
    public async Task<bool> ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning(
                "Action={Action} Email={Email} Status={Status} Reason={Reason}",
                "ResetPassword", email, "FAIL", "UserNotFound");
            return false;
        }

        var resetToken = await _userRepository.GetEmailVerificationTokenAsync(token);
        if (resetToken == null || resetToken.UserId != user.Id)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "ResetPassword", user.Id, email, "FAIL", "InvalidToken");
            return false;
        }

        // Check if already used (race condition protection) - BEFORE expiry check
        if (resetToken.IsUsed)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "ResetPassword", user.Id, email, "FAIL", "TokenAlreadyUsed");
            return false;
        }

        // Check expiry AFTER checking if used
        // NOTE: Using <= instead of < (JWT standard) to treat exactly-expired tokens as invalid
        // This is intentionally more strict for security - tokens expire at the exact moment, not after
        if (resetToken.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "ResetPassword", user.Id, email, "FAIL", "TokenExpired");
            return false;
        }

        // Validate new password strength
        PasswordValidator.Validate(newPassword);

        // Mark token as used BEFORE updating password (atomic operation)
        resetToken.IsUsed = true;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        // Both updates happen in same transaction
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Email={Email} Status={Status}",
            "ResetPassword", user.Id, email, "SUCCESS");

        return true;
    }

    // ── VERIFY EMAIL ─────────────────────────────────────────────────────────────
    public async Task<bool> VerifyEmailAsync(string token)
    {
        var record = await _userRepository.GetEmailVerificationTokenAsync(token);

        if (record == null || record.IsUsed || record.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "Action={Action} Status={Status} Reason={Reason}",
                "VerifyEmail", "FAIL", "InvalidOrExpiredToken");
            return false;
        }

        record.IsUsed = true;
        record.User.IsEmailVerified = true;

        await _userRepository.UpdateAsync(record.User);

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Email={Email} Status={Status}",
            "VerifyEmail", record.UserId, record.User.Email, "SUCCESS");

        return true;
    }

    // ── LOGOUT ───────────────────────────────────────────────────────────────────
    public async Task<bool> LogoutAsync(string refreshToken, int userId)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ValidationException("Refresh token is required.");

        var token = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

        if (token == null)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Status={Status} Reason={Reason}",
                "Logout", userId, "FAIL", "TokenNotFound");
            throw new NotFoundException("Token not found.");
        }

        // Verify token ownership
        if (token.UserId != userId)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} TokenUserId={TokenUserId} Status={Status} Reason={Reason}",
                "Logout", userId, token.UserId, "FAIL", "InvalidTokenOwnership");
            throw new UnauthorizedException("Invalid token ownership.");
        }

        // Check if already revoked
        if (token.IsRevoked)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Status={Status} Reason={Reason}",
                "Logout", userId, "FAIL", "TokenAlreadyRevoked");
            throw new ValidationException("Token already revoked.");
        }

        // Revoke the token
        token.IsRevoked = true;
        await _refreshTokenRepository.UpdateAsync(token);

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Status={Status}",
            "Logout", userId, "SUCCESS");

        // Publish event for audit logging
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = userId,
            Action = "Logout",
            EventType = "SUCCESS",
            CorrelationId = GetCorrelationId()
        });

        return true;
    }

    // ── LOGOUT ALL ───────────────────────────────────────────────────────────────
    public async Task LogoutAllAsync(int userId)
    {
        await _tokenService.RevokeAllTokensAsync(userId);

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Status={Status}",
            "LogoutAll", userId, "SUCCESS");

        // Publish event for audit logging
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = userId,
            Action = "LogoutAll",
            EventType = "SUCCESS",
            CorrelationId = GetCorrelationId()
        });
    }

    // ── RESEND VERIFICATION EMAIL ────────────────────────────────────────────────
    public async Task<string?> ResendVerificationEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        
        // Silent fail to prevent email enumeration
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning(
                "Action={Action} Email={Email} Status={Status} Reason={Reason}",
                "ResendVerification", email, "FAIL", "UserNotFound");
            return null;
        }

        // Don't resend if already verified
        if (user.IsEmailVerified)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "ResendVerification", user.Id, email, "FAIL", "AlreadyVerified");
            return null;
        }

        // Generate new verification token
        var verificationToken = new EmailVerificationToken
        {
            UserId = user.Id,
            Token = GenerateOtp(),
            ExpiresAt = DateTime.UtcNow.AddHours(SecurityConstants.EmailVerificationTokenExpiryHours),
            IsUsed = false
        };

        await _userRepository.AddEmailVerificationTokenAsync(verificationToken);

        // Send verification email
        try
        {
            await _emailService.SendVerificationEmailAsync(user.Email, user.Name, verificationToken.Token);
            _logger.LogInformation("Verification email resent to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
            _logger.LogWarning("Continuing without sending verification email; user will need to retry.");
        }

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Email={Email} Status={Status}",
            "ResendVerification", user.Id, email, "SUCCESS");

        return verificationToken.Token;
    }

    // ── CHANGE PASSWORD ──────────────────────────────────────────────────────────
    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Status={Status} Reason={Reason}",
                "ChangePassword", userId, "FAIL", "UserNotFound");
            return false;
        }

        // Verify current password
        bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
        
        if (!isCurrentPasswordValid)
        {
            _logger.LogWarning(
                "Action={Action} UserId={UserId} Email={Email} Status={Status} Reason={Reason}",
                "ChangePassword", user.Id, user.Email, "FAIL", "InvalidCurrentPassword");
            return false;
        }

        // Validate new password strength
        PasswordValidator.Validate(newPassword);

        // Ensure new password is different from current
        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
        {
            throw new ValidationException("New password must be different from current password.");
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Email={Email} Status={Status}",
            "ChangePassword", user.Id, user.Email, "SUCCESS");

        // Publish event for audit logging
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = user.Id,
            Action = "ChangePassword",
            EventType = "SUCCESS",
            Email = user.Email,
            CorrelationId = GetCorrelationId()
        });

        return true;
    }
}
