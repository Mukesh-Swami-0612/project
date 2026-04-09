using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Application.Services;
using Ecom.Auth.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace Ecom.Auth.Tests.Unit;

[TestFixture]
public class TokenServiceTests
{
    private TokenService _tokenService = null!;

    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-that-is-long-enough-32chars!",
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();

        var refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();

        _tokenService = new TokenService(config, refreshTokenRepoMock.Object);
    }

    [Test]
    public void GenerateAccessToken_ValidUser_ReturnsNonEmptyToken()
    {
        var user = new User { Id = 1, Email = "test@test.com", Role = new Role { RoleName = "Admin" } };
        var token = _tokenService.GenerateAccessToken(user);
        Assert.That(token, Is.Not.Empty);
    }

    [Test]
    public void GenerateRefreshToken_ReturnsUniqueTokens()
    {
        var t1 = _tokenService.GenerateRefreshToken();
        var t2 = _tokenService.GenerateRefreshToken();
        Assert.That(t1, Is.Not.EqualTo(t2));
    }
}
