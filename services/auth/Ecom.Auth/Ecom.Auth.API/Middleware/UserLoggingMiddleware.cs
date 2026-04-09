using Serilog.Context;
using System.Security.Claims;

namespace Ecom.Auth.API.Middleware;

public class UserLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public UserLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var user = context.User;

        var userId = user?.FindFirst("UserId")?.Value;
        var email = user?.FindFirst(ClaimTypes.Email)?.Value;

        using (LogContext.PushProperty("UserId", userId ?? "Anonymous"))
        using (LogContext.PushProperty("Email", email ?? "Anonymous"))
        {
            await _next(context);
        }
    }
}
