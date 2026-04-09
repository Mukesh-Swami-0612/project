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
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _env;

    public AuthController(IAuthService authService, IHostEnvironment env)
    {
        _authService = authService;
        _env = env;
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
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login successful"));
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
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request.Token);
        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Token refreshed successfully"));
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
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid user token"));
        }

        await _authService.LogoutAsync(request.Token, userId);

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

}
