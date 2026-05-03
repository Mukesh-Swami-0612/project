using Ecom.Auth.Application.DTOs;

namespace Ecom.Auth.Application.Interfaces;

/// <summary>
/// Service for user management operations (admin functions).
/// </summary>
public interface IUserManagementService
{
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    Task<UserResponseDto> GetUserByIdAsync(int id);
    Task UpdateUserStatusAsync(int id, bool isActive, int currentUserId);
    Task ChangeUserRoleAsync(int id, int roleId, int currentUserId);
    Task UnlockUserAsync(int id);
    Task DeleteUserAsync(int id, int currentUserId);
}
