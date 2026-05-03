namespace Ecom.Auth.Application.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsDeleted { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}
