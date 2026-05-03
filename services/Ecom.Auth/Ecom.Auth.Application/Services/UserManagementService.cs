using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Exceptions;
using Ecom.Auth.Application.Interfaces;
using Ecom.Shared.Contracts.Events;
using Ecom.Shared.Contracts.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ecom.Auth.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserManagementService> _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserManagementService(
        IUserRepository userRepository,
        ITokenService tokenService,
        ILogger<UserManagementService> logger,
        IEventPublisher eventPublisher,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
        _eventPublisher = eventPublisher;
        _httpContextAccessor = httpContextAccessor;
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

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        
        return users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            RoleId = u.RoleId,
            Role = u.Role?.RoleName,
            IsActive = u.IsActive,
            IsEmailVerified = u.IsEmailVerified,
            IsDeleted = u.IsDeleted,
            FailedLoginAttempts = u.FailedLoginAttempts,
            LockoutEnd = u.LockoutEnd,
            CreatedAt = u.CreatedAt
        });
    }

    public async Task<UserResponseDto> GetUserByIdAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", id);

        return new UserResponseDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            RoleId = user.RoleId,
            Role = user.Role?.RoleName,
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            IsDeleted = user.IsDeleted,
            FailedLoginAttempts = user.FailedLoginAttempts,
            LockoutEnd = user.LockoutEnd,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task UpdateUserStatusAsync(int id, bool isActive, int currentUserId)
    {
        // Business rule: admin cannot deactivate themselves
        if (id == currentUserId)
            throw new ValidationException("You cannot change your own account status.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", id);

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Action={Action} TargetUserId={TargetUserId} AdminUserId={AdminUserId} IsActive={IsActive} Status={Status}",
            "UpdateUserStatus", id, currentUserId, isActive, "SUCCESS");

        // Publish event for audit logging
        var activity = System.Diagnostics.Activity.Current;
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = id,
            Action = "UpdateUserStatus",
            EventType = nameof(UserActionEvent),
            AdditionalInfo = $"Status: SUCCESS, IsActive: {isActive}, AdminUserId: {currentUserId}",
            CorrelationId = GetCorrelationId(),
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString()
        }, "user.action");
    }

    public async Task ChangeUserRoleAsync(int id, int roleId, int currentUserId)
    {
        if (id == currentUserId)
            throw new ValidationException("You cannot change your own role.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", id);

        // Validate role exists in database
        var roleExists = await _userRepository.RoleExistsAsync(roleId);
        if (!roleExists)
            throw new ValidationException("Invalid role specified.");

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Action={Action} TargetUserId={TargetUserId} AdminUserId={AdminUserId} NewRoleId={NewRoleId} Status={Status}",
            "ChangeUserRole", id, currentUserId, roleId, "SUCCESS");

        // Publish event for audit logging
        var activity = System.Diagnostics.Activity.Current;
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = id,
            Action = "ChangeUserRole",
            EventType = nameof(UserActionEvent),
            AdditionalInfo = $"Status: SUCCESS, NewRoleId: {roleId}, AdminUserId: {currentUserId}",
            CorrelationId = GetCorrelationId(),
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString()
        }, "user.action");
    }

    public async Task UnlockUserAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            throw new NotFoundException("User", id);

        user.LockoutEnd = null;
        user.FailedLoginAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation(
            "Action={Action} UserId={UserId} Status={Status}",
            "UnlockUser", id, "SUCCESS");

        // Publish event for audit logging
        var activity = System.Diagnostics.Activity.Current;
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = id,
            Action = "UnlockUser",
            EventType = nameof(UserActionEvent),
            AdditionalInfo = "Status: SUCCESS",
            CorrelationId = GetCorrelationId(),
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString()
        }, "user.action");
    }

    public async Task DeleteUserAsync(int id, int currentUserId)
    {
        // Business rule: admin cannot delete themselves
        if (id == currentUserId)
            throw new ValidationException("You cannot delete your own account.");

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null || user.IsDeleted)
            throw new ValidationException("User already deleted or not found.");

        // Soft delete + revoke all active sessions
        user.IsDeleted = true;
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);

        // Revoke all refresh tokens for this user
        await _tokenService.RevokeAllTokensAsync(id);

        _logger.LogInformation(
            "Action={Action} TargetUserId={TargetUserId} AdminUserId={AdminUserId} Status={Status}",
            "DeleteUser", id, currentUserId, "SUCCESS");

        // Publish event for audit logging
        var activity = System.Diagnostics.Activity.Current;
        await _eventPublisher.PublishAsync(new UserActionEvent
        {
            EntityId = id,
            Action = "DeleteUser",
            EventType = nameof(UserActionEvent),
            AdditionalInfo = $"Status: SUCCESS, AdminUserId: {currentUserId}",
            CorrelationId = GetCorrelationId(),
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString()
        }, "user.action");
    }
}
