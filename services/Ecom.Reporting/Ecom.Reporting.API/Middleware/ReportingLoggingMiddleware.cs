using Serilog.Context;

namespace Ecom.Reporting.API.Middleware;

public class ReportingLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ReportingLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // ZERO TRUST: Extract username from JWT claims, not gateway headers
        var username = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                      ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? context.User?.FindFirst("sub")?.Value
                      ?? "System";
        var endpoint = context.Request.Path;
        var correlationId = context.TraceIdentifier;

        using (LogContext.PushProperty("Username", username))
        using (LogContext.PushProperty("Endpoint", endpoint))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
