using Serilog.Context;

namespace Ecom.Notification.API.Middleware;

/// <summary>
/// Middleware to enrich logs with contextual information
/// Adds Username and CorrelationId to all log entries
/// </summary>
public class LoggingContextMiddleware
{
    private readonly RequestDelegate _next;

    public LoggingContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ZERO TRUST: Extract username from JWT claims, not gateway headers
        var username = context.User?.Identity?.Name 
                      ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                      ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? context.User?.FindFirst("sub")?.Value
                      ?? "System";

        // Use TraceIdentifier as CorrelationId
        var correlationId = context.TraceIdentifier;

        // Push properties to Serilog LogContext
        using (LogContext.PushProperty("Username", username))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class LoggingContextMiddlewareExtensions
{
    public static IApplicationBuilder UseLoggingContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingContextMiddleware>();
    }
}
