using Asp.Versioning;
using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Ecom.Auth.API.Controllers;

/// <summary>
/// User management endpoints for administrators
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/auth/users")]
[ApiVersion("1.0")]
[Authorize(Roles = Roles.Admin)]
[EnableRateLimiting("admin")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // ── GET ALL USERS ─────────────────────────────────────────────────────────
    /// <summary>
    /// Retrieves all users in the system
    /// </summary>
    /// <returns>List of all users with their details</returns>
    /// <response code="200">Users retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponseDto>>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userManagementService.GetAllUsersAsync();
        return Ok(ApiResponse<IEnumerable<UserResponseDto>>.SuccessResponse(users, "Users retrieved successfully"));
    }

    // ── GET USER BY ID ────────────────────────────────────────────────────────
    /// <summary>
    /// Retrieves a specific user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    /// <response code="200">User retrieved successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userManagementService.GetUserByIdAsync(id);
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(user, "User retrieved successfully"));
    }

    // ── ACTIVATE / DEACTIVATE USER ────────────────────────────────────────────
    /// <summary>
    /// Activates or deactivates a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">Status update details</param>
    /// <returns>Status update confirmation</returns>
    /// <response code="200">User status updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusDto dto)
    {
        await _userManagementService.UpdateUserStatusAsync(id, dto.IsActive, CurrentUserId);
        var message = $"User {(dto.IsActive ? "activated" : "deactivated")} successfully";
        return Ok(ApiResponse<object>.SuccessResponse(null!, message));
    }

    // ── CHANGE ROLE ───────────────────────────────────────────────────────────
    /// <summary>
    /// Changes a user's role
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="dto">New role details</param>
    /// <returns>Role change confirmation</returns>
    /// <response code="200">User role updated successfully</response>
    /// <response code="400">Invalid role ID</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}/role")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
    {
        await _userManagementService.ChangeUserRoleAsync(id, dto.RoleId, CurrentUserId);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "User role updated successfully"));
    }

    // ── UNLOCK USER ───────────────────────────────────────────────────────────
    /// <summary>
    /// Unlocks a user account that was locked due to failed login attempts
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Unlock confirmation</returns>
    /// <response code="200">User account unlocked successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpPut("{id}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> UnlockUser(int id)
    {
        await _userManagementService.UnlockUserAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "User account unlocked successfully"));
    }

    // ── SOFT DELETE USER ──────────────────────────────────────────────────────
    /// <summary>
    /// Soft deletes a user account (marks as deleted without removing from database)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Deletion confirmation</returns>
    /// <response code="200">User deleted successfully</response>
    /// <response code="401">Unauthorized - JWT token required</response>
    /// <response code="403">Forbidden - Admin role required</response>
    /// <response code="404">User not found</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _userManagementService.DeleteUserAsync(id, CurrentUserId);
        return Ok(ApiResponse<object>.SuccessResponse(null!, "User deleted successfully"));
    }
}
