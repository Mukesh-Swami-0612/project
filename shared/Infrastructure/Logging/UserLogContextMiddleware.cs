using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Security.Claims;

namespace Ecom.Shared.Infrastructure.Logging;

public class UserLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User?.FindFirst("UserId")?.Value
            ?? context.User?.FindFirst("userId")?.Value
            ?? context.User?.FindFirst("sub")?.Value
            ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            await _next(context);
            return;
        }

        using (LogContext.PushProperty("UserId", userId))
        {
            await _next(context);
        }
    }
}
