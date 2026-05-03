using System.Security.Claims;
using Ecom.Auth.Application.Interfaces;

namespace Ecom.Auth.API.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, IAuditService auditService)
    {
        // Skip logging for Swagger and Health checks
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var user = context.User;

        var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user?.FindFirst(ClaimTypes.Email)?.Value;

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var path = context.Request.Path;
        var method = context.Request.Method;

        // Convert path to clean action name
        string action = path.Value switch
        {
            "/api/v1/auth/login" => "LOGIN",
            "/api/v1/auth/signup" => "SIGNUP",
            "/api/v1/auth/logout" => "LOGOUT",
            "/api/v1/auth/logout-all" => "LOGOUT_ALL",
            "/api/v1/auth/refresh" => "REFRESH_TOKEN",
            "/api/v1/auth/forgot-password" => "FORGOT_PASSWORD",
            "/api/v1/auth/reset-password" => "RESET_PASSWORD",
            "/api/v1/auth/verify-email" => "VERIFY_EMAIL",
            "/api/v1/auth/resend-verification" => "RESEND_VERIFICATION",
            "/api/v1/auth/change-password" => "CHANGE_PASSWORD",
            "/api/v1/auth/me" => "GET_PROFILE",
            _ => $"{method} {path}"
        };

        string status = "SUCCESS";

        try
        {
            await _next(context);

            if (context.Response.StatusCode >= 400)
            {
                status = "FAILED";
            }
        }
        catch
        {
            status = "FAILED";
            throw;
        }
        finally
        {
            await auditService.LogAsync(
                string.IsNullOrEmpty(userId) ? null : int.Parse(userId),
                email ?? "Anonymous",
                action,
                status,
                ip
            );
        }
    }
}
