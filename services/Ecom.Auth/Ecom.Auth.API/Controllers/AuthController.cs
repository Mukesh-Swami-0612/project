using Asp.Versioning;
using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.Extensions.Hosting;

namespace Ecom.Auth.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _env;

    public AuthController(IAuthService authService, IHostEnvironment env)
    {
        _authService = authService;
        _env = env;
    }

    // ── COOKIE HELPERS ───────────────────────────────────────────────────────────
    private void SetTokenCookies(string accessToken, string refreshToken)
    {
        var isDevelopment = _env.IsDevelopment();
        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment, // false in dev, true in prod
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(7) // Refresh token expiry
        };

        var accessCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment, // false in dev, true in prod
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(15) // Access token expiry
        };

        Response.Cookies.Append("accessToken", accessToken, accessCookieOptions);
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }

    private void ClearTokenCookies()
    {
        var isDevelopment = _env.IsDevelopment();
        
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment, // false in dev, true in prod
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.Strict,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(-1) // Expire immediately
        };

        Response.Cookies.Append("accessToken", "", cookieOptions);
        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }

    // ── SIGNUP ───────────────────────────────────────────────────────────────────
    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <returns>User information. Verification code is sent only to the registered email.</returns>
    /// <response code="200">Registration successful</response>
    /// <response code="400">Invalid input or validation error</response>
    /// <response code="409">Email already registered</response>
    /// <remarks>
    /// Password must meet security requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// 
    /// Verification code is sent only via email and is not returned in response body.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("signup")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 409)]
    public async Task<IActionResult> Signup([FromBody] SignupRequestDto request)
    {
        var (userDto, verificationToken) = await _authService.SignupAsync(request);

        object responseData = new { userId = userDto.Id, email = userDto.Email };

        return Ok(ApiResponse<object>.SuccessResponse(
            responseData,
            "Registration successful. Please check your email for the verification code."));
    }

    // ── VERIFY EMAIL ────────────────────────────────────────────────────────────
    /// <summary>
    /// Verifies user email address using verification token
    /// </summary>
    /// <param name="token">Email verification code sent to user's email</param>
    /// <returns>Verification result</returns>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid or expired verification token</response>
    [AllowAnonymous]
    [HttpGet("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var success = await _authService.VerifyEmailAsync(token);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid or expired verification code."));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Email verified successfully. You can now log in."));
    }

    // ── RESEND VERIFICATION EMAIL ────────────────────────────────────────────────
    /// <summary>
    /// Resends email verification code to user
    /// </summary>
    /// <param name="request">Email address to resend verification to</param>
    /// <returns>Confirmation message</returns>
    /// <response code="200">Verification email sent (if email exists)</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <remarks>
    /// Returns generic success message to prevent email enumeration attacks.
    /// Verification code is sent only via email and is never returned in API response.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("resend-verification")]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ForgotPasswordRequestDto request)
    {
        var token = await _authService.ResendVerificationEmailAsync(request.Email);

        // Always return generic success to prevent email enumeration
        return Ok(ApiResponse<object>.SuccessResponse(
            null!,
            "If the email exists and is not verified, a verification code was sent"));
    }

    // ── LOGIN ────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Authenticates user and returns JWT access token with refresh token
    /// </summary>
    /// <param name="request">User login credentials</param>
    /// <returns>JWT access token, refresh token, expiration time, and user role</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid email or password</response>
    /// <response code="401">Email not verified or account locked</response>
    /// <response code="429">Too many login attempts - rate limit exceeded</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        
        // Return tokens in response body (Authorization header approach)
        var responseData = new LoginResponseDto
        {
            AccessToken = result.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresAt = result.ExpiresAt,
            Role = result.Role
        };
        
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(responseData, "Login successful"));
    }

    // ── REFRESH TOKEN ────────────────────────────────────────────────────────────
    /// <summary>
    /// Refreshes expired JWT access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request containing the token</param>
    /// <returns>New JWT access token and refresh token pair</returns>
    /// <response code="200">Token refreshed successfully</response>
    /// <response code="400">Invalid or expired refresh token</response>
    /// <response code="401">Token has been revoked or reused</response>
    /// <response code="429">Too many refresh attempts - rate limit exceeded</response>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> Refresh()
    {
        // Get refresh token from HTTP-only cookie
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("No refresh token provided"));
        }
        
        var result = await _authService.RefreshTokenAsync(refreshToken);
        
        // Set new tokens in HTTP-only cookies
        SetTokenCookies(result.AccessToken, result.RefreshToken);
        
        // Return response without tokens (they're in cookies now)
        var responseData = new LoginResponseDto
        {
            ExpiresAt = result.ExpiresAt,
            Role = result.Role
            // AccessToken and RefreshToken removed - now in cookies
        };
        
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(responseData, "Token refreshed successfully"));
    }

    // ── LOGOUT ───────────────────────────────────────────────────────────────────
    /// <summary>
    /// Logs out user from current session by revoking refresh token
    /// </summary>
    /// <param name="request">Refresh token request containing the token to revoke</param>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">Logged out successfully</response>
    /// <response code="400">Token not found or already revoked</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token"));
        }

        // Get refresh token from HTTP-only cookie
        var refreshToken = Request.Cookies["refreshToken"];
        
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken, userId);
        }

        // Clear HTTP-only cookies
        ClearTokenCookies();

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Logged out successfully"));
    }

    // ── LOGOUT ALL ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Logs out user from all sessions by revoking all refresh tokens
    /// </summary>
    /// <returns>Logout confirmation</returns>
    /// <response code="200">Logged out from all devices successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [Authorize]
    [HttpPost("logout-all")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LogoutAll()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token"));
        }

        await _authService.LogoutAllAsync(userId);

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Logged out from all devices"));
    }

    // ── FORGOT PASSWORD ───────────────────────────────────────────────────────────
    /// <summary>
    /// Initiates password reset process by generating a 6-digit reset code
    /// </summary>
    /// <param name="request">Email address for password reset</param>
    /// <returns>Password reset confirmation (token sent via email in production)</returns>
    /// <response code="200">Password reset initiated - check email for reset link</response>
    /// <response code="429">Too many password reset attempts - rate limit exceeded</response>
    /// <remarks>
    /// Returns generic success message to prevent email enumeration attacks.
    /// In production, reset code is sent via email, not in response body.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(429)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var token = await _authService.ForgotPasswordAsync(request.Email);

        // Always return generic success to prevent email enumeration
        return Ok(ApiResponse<object>.SuccessResponse(
            null!,
            "If the email exists, a reset code was sent"));
    }

    // ── RESET PASSWORD ────────────────────────────────────────────────────────────
    /// <summary>
    /// Resets user password using reset token
    /// </summary>
    /// <param name="request">Email, reset code, and new password</param>
    /// <returns>Password reset confirmation</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Invalid email, token, or token has expired</response>
    /// <remarks>
    /// New password must meet security requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        var success = await _authService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Invalid email, code, or the code has expired"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Password reset successfully"));
    }

    // ── CURRENT USER ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Gets current authenticated user information from JWT token
    /// </summary>
    /// <returns>User ID, email, and role</returns>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(401)]
    public IActionResult GetCurrentUser()
    {
        var userData = new
        {
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            role = User.FindFirst(ClaimTypes.Role)?.Value
        };

        return Ok(ApiResponse<object>.SuccessResponse(userData, "User information retrieved successfully"));
    }

    // ── CHANGE PASSWORD ───────────────────────────────────────────────────────────
    /// <summary>
    /// Changes password for authenticated user
    /// </summary>
    /// <param name="request">Current password and new password</param>
    /// <returns>Password change confirmation</returns>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid current password or validation error</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <remarks>
    /// New password must meet security requirements:
    /// - Minimum 8 characters
    /// - At least one uppercase letter
    /// - At least one lowercase letter
    /// - At least one digit
    /// - At least one special character
    /// </remarks>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token"));
        }

        var success = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Current password is incorrect"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Password changed successfully"));
    }

    // ── SESSION MANAGEMENT ───────────────────────────────────────────────────────

    /// <summary>
    /// Get all active sessions for the current user
    /// </summary>
    /// <returns>List of active sessions with device information</returns>
    /// <response code="200">Returns list of active sessions</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [Authorize]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<List<SessionDto>>), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSessions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token"));
        }

        var sessions = await _authService.GetActiveSessionsAsync(userId);

        return Ok(ApiResponse<List<SessionDto>>.SuccessResponse(sessions, "Sessions retrieved successfully"));
    }

    /// <summary>
    /// Revoke a specific session by session ID
    /// </summary>
    /// <param name="sessionId">The ID of the session to revoke</param>
    /// <returns>Revocation confirmation</returns>
    /// <response code="200">Session revoked successfully</response>
    /// <response code="400">Session not found or already revoked</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    [Authorize]
    [HttpPost("revoke-session/{sessionId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> RevokeSession(int sessionId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token"));
        }

        var success = await _authService.RevokeSessionAsync(userId, sessionId);

        if (!success)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Session not found or already revoked"));
        }

        return Ok(ApiResponse<object>.SuccessResponse(null!, "Session revoked successfully"));
    }

}
