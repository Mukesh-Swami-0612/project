using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Ecom.Shared.Infrastructure.Configuration;

/// <summary>
/// 🔥 CORS: Shared CORS configuration for consistent policy across all services
/// </summary>
public static class CorsConfiguration
{
    public const string PolicyName = "AllowAngular";

    /// <summary>
    /// Add consistent CORS policy to services
    /// </summary>
    public static IServiceCollection AddEcomCors(this IServiceCollection services)
    {
        return services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
                policy.WithOrigins(
                    "http://localhost:4200",   // Angular dev server
                    "https://localhost:4200",  // Angular dev server (HTTPS)
                    "http://localhost:3000",   // Alternative dev port
                    "https://localhost:3000"   // Alternative dev port (HTTPS)
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()  // 🔥 SECURITY: Required for HTTP-only cookies
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10))); // Cache preflight for 10 minutes
        });
    }

    /// <summary>
    /// Use CORS middleware with consistent policy
    /// </summary>
    public static IApplicationBuilder UseEcomCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }

    /// <summary>
    /// Get allowed origins for configuration
    /// </summary>
    public static string[] GetAllowedOrigins()
    {
        return new[]
        {
            "http://localhost:4200",
            "https://localhost:4200",
            "http://localhost:3000",
            "https://localhost:3000"
        };
    }

    /// <summary>
    /// Validate if origin is allowed
    /// </summary>
    public static bool IsOriginAllowed(string origin)
    {
        var allowedOrigins = GetAllowedOrigins();
        return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
    }
}