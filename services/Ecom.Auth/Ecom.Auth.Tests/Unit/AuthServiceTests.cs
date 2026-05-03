using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Exceptions;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Application.Services;
using Ecom.Auth.Domain.Entities;
using Ecom.Shared.Contracts.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Ecom.Auth.Tests.Unit;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserRepository> _userRepoMock = null!;
    private Mock<ITokenService> _tokenServiceMock = null!;
    private Mock<IRefreshTokenRepository> _refreshTokenRepoMock = null!;
    private Mock<ILogger<AuthService>> _loggerMock = null!;
    private Mock<IConfiguration> _configMock = null!;
    private Mock<IEventPublisher> _eventPublisherMock = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<Microsoft.Extensions.Hosting.IHostEnvironment> _envMock = null!;
    private Mock<IDbContextTransaction> _transactionMock = null!;
    private AuthService _authService = null!;

    [SetUp]
    public void Setup()
    {
        _userRepoMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _configMock = new Mock<IConfiguration>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _emailServiceMock = new Mock<IEmailService>();
        _envMock = new Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        _transactionMock = new Mock<IDbContextTransaction>();

        // Setup configuration mock for JWT settings
        _configMock.Setup(c => c["Jwt:ExpiryMinutes"]).Returns("15");
        _configMock.Setup(c => c["Jwt:RefreshTokenExpiryDays"]).Returns("7");

        // Setup HttpContext with CorrelationId
        var httpContext = new DefaultHttpContext();
        httpContext.Items["CorrelationId"] = Guid.NewGuid().ToString();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Setup event publisher - explicitly provide routingKey to avoid optional parameter issue
        _eventPublisherMock.Setup(e => e.PublishAsync(It.IsAny<object>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(_transactionMock.Object);
        _userRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.RoleExistsAsync(Roles.CustomerId)).ReturnsAsync(true);
        _emailServiceMock.Setup(e => e.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _authService = new AuthService(
            _userRepoMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepoMock.Object,
            _loggerMock.Object,
            _configMock.Object,
            _eventPublisherMock.Object,
            _httpContextAccessorMock.Object,
            _emailServiceMock.Object,
            _envMock.Object
        );
    }

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
            IsActive = true,
            IsEmailVerified = true, // Required: email must be verified to login
            Role = new Role { RoleName = "Admin" }
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
        _tokenServiceMock.Setup(t => t.GenerateAccessToken(user)).Returns("access-token");
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        _tokenServiceMock.Setup(t => t.SaveRefreshTokenAsync(user.Id, "refresh-token"))
            .ReturnsAsync(new RefreshToken { TokenHash = "hashed-refresh-token" });

        // Act
        var result = await _authService.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "Password1" });

        // Assert
        Assert.That(result.AccessToken, Is.EqualTo("access-token"));
        Assert.That(result.RefreshToken, Is.EqualTo("refresh-token"));
    }

    [Test]
    public void LoginAsync_UnverifiedEmail_ThrowsUnauthorized()
    {
        // Arrange — email not verified
        var user = new User
        {
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
            IsActive = true,
            IsEmailVerified = false, // Email not verified
            Role = new Role { RoleName = "User" }
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        // Act + Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "Password1" }));
    }

    [Test]
    public void LoginAsync_InvalidPassword_ThrowsUnauthorized()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPass1"),
            IsActive = true,
            IsEmailVerified = true,
            Role = new Role { RoleName = "Admin" }
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);
        _userRepoMock.Setup(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

        // Act + Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "WrongPass" }));
    }

    [Test]
    public void LoginAsync_LockedOutUser_ThrowsUnauthorized()
    {
        // Arrange — user is locked out
        var user = new User
        {
            Id = 1,
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1"),
            IsActive = true,
            IsEmailVerified = true,
            LockoutEnd = DateTime.UtcNow.AddMinutes(10), // Still locked
            Role = new Role { RoleName = "User" }
        };
        _userRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(user);

        // Act + Assert
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.LoginAsync(new LoginRequestDto { Email = "test@test.com", Password = "Password1" }));
    }

    [Test]
    public async Task SignupAsync_AlwaysAssignsCustomerRole()
    {
        User? createdUser = null;

        _userRepoMock.Setup(r => r.GetByEmailAsync("signup@test.com")).ReturnsAsync((User?)null);
        _userRepoMock.Setup(r => r.AddAsync(It.IsAny<User>(), false))
            .Callback<User, bool>((user, _) => createdUser = user)
            .Returns(Task.CompletedTask);
        _userRepoMock.Setup(r => r.AddEmailVerificationTokenAsync(It.IsAny<EmailVerificationToken>(), false))
            .Returns(Task.CompletedTask);

        await _authService.SignupAsync(new SignupRequestDto
        {
            Name = "Signup User",
            Email = "signup@test.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        });

        Assert.That(createdUser, Is.Not.Null);
        Assert.That(createdUser!.RoleId, Is.EqualTo(Roles.CustomerId));
    }
}
