using System.Net.Http.Json;
using System.Text;
using Ecom.Auth.Application.Common;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.API.Controllers;
using Ecom.Auth.Infrastructure.Persistence;
using Ecom.Shared.Contracts.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Ecom.Auth.Tests.Integration;

[TestFixture]
public class AuthIntegrationTests
{
    private HttpClient _client = null!;
    private WebApplicationFactory<Program> _factory = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("JWT_KEY", "test-super-secret-key-that-is-long-enough");
        Environment.SetEnvironmentVariable("SMTP_PASSWORD", "test-smtp-password");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AuthDbContext>));
                services.RemoveAll(typeof(IEventPublisher));
                services.RemoveAll(typeof(IEmailService));
                
                services.AddDbContext<AuthDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                    options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
                services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
                services.AddSingleton<IEmailService, NoOpEmailService>();
            });
        });
        
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task Signup_Should_Return_Success()
    {
        var email = $"signup_{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email);
    }

    [Test]
    public async Task Signup_Should_Force_Customer_Role()
    {
        var email = $"customer_{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        user.RoleId.Should().Be(Roles.CustomerId);
    }

    [Test]
    public async Task Signup_Should_Reject_RoleId_In_Request_Body()
    {
        EnsureRolesSeeded();

        var email = $"reject_{Guid.NewGuid()}@example.com";
        const string password = "TestPassword@1234!";
        var requestJson = $$"""
        {
          "name": "Malicious User",
          "email": "{{email}}",
          "password": "{{password}}",
          "confirmPassword": "{{password}}",
          "roleId": 1
        }
        """;

        using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/v1/auth/signup", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, responseContent);
        responseContent.Should().Contain("Role assignment is not allowed during signup.");
    }

    [Test]
    public async Task Login_Should_Return_Token_When_Valid()
    {
        var email = $"login_{Guid.NewGuid()}@example.com";
        var password = "TestPassword@1234!";
        await RegisterUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"Login should succeed, got {response.StatusCode}: {responseContent}");
        responseContent.Should().Contain("accessToken");
    }

    [Test]
    public async Task RefreshToken_Should_Return_New_Tokens()
    {
        var email = $"refresh_{Guid.NewGuid()}@example.com";
        var password = "TestPassword@1234!";
        await RegisterUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        
        loginContent.Should().NotBeNull();
        loginContent!.Data.Should().NotBeNull();

        var refreshRequest = new RefreshTokenRequestDto
        {
            Token = loginContent.Data.RefreshToken
        };

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);
        var refreshContent = await refreshResponse.Content.ReadAsStringAsync();

        refreshResponse.IsSuccessStatusCode.Should().BeTrue($"Refresh should succeed, got {refreshResponse.StatusCode}: {refreshContent}");
        refreshContent.Should().Contain("accessToken");
        refreshContent.Should().Contain("refreshToken");
    }

    [Test]
    public async Task ForgotPassword_Should_Return_Success()
    {
        var email = $"forgot_{Guid.NewGuid()}@example.com";
        await RegisterUserAsync(email);

        var forgotRequest = new ForgotPasswordRequestDto
        {
            Email = email
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", forgotRequest);
        var responseContent = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"Forgot password should succeed, got {response.StatusCode}: {responseContent}");
    }

    [Test]
    public void UsersController_Should_Require_Admin_Role()
    {
        var authorizeAttribute = typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttribute.Should().NotBeNull();
        authorizeAttribute!.Roles.Should().Be(Roles.Admin);
    }

    private async Task RegisterUserAsync(string email, string password = "TestPassword@1234!")
    {
        EnsureRolesSeeded();

        var request = new SignupRequestDto
        {
            Name = "Test User",
            Email = email,
            Password = password,
            ConfirmPassword = password
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", request);
        var responseContent = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue($"Signup should succeed for {email}, but got {response.StatusCode}: {responseContent}");

        // Auto-verify email in database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            var user = db.Users.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                user.IsEmailVerified = true;
                db.SaveChanges();
            }
        }
    }

    private void EnsureRolesSeeded()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        if (db.Roles.Any())
            return;

        db.Roles.AddRange(
            new Ecom.Auth.Domain.Entities.Role { Id = Roles.AdminId, RoleName = Roles.Admin },
            new Ecom.Auth.Domain.Entities.Role { Id = Roles.ProductManagerId, RoleName = Roles.ProductManager },
            new Ecom.Auth.Domain.Entities.Role { Id = Roles.ContentExecutiveId, RoleName = Roles.ContentExecutive },
            new Ecom.Auth.Domain.Entities.Role { Id = Roles.CustomerId, RoleName = Roles.Customer }
        );
        db.SaveChanges();
    }

    private sealed class NoOpEventPublisher : IEventPublisher
    {
        public Task PublishAsync<T>(T @event, string routingKey = "") where T : class => Task.CompletedTask;
    }

    private sealed class NoOpEmailService : IEmailService
    {
        public Task SendVerificationEmailAsync(string toEmail, string userName, string verificationToken) => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken) => Task.CompletedTask;
    }
}
