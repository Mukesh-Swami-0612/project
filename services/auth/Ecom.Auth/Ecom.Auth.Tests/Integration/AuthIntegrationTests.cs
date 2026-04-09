using System.Net.Http.Json;
using Ecom.Auth.Application.DTOs;
using Ecom.Auth.Infrastructure.Persistence;
using FluentAssertions;
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
                
                services.AddDbContext<AuthDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                    options.ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });
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

    private async Task RegisterUserAsync(string email, string password = "TestPassword@1234!")
    {
        // Seed Role into InMemory DB
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            if (!db.Roles.Any(r => r.Id == 1))
            {
                db.Roles.Add(new Ecom.Auth.Domain.Entities.Role { Id = 1, RoleName = "User" });
                db.SaveChanges();
            }
        }

        var request = new SignupRequestDto
        {
            Name = "Test User",
            Email = email,
            Password = password,
            ConfirmPassword = password,
            RoleId = 1
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
}
