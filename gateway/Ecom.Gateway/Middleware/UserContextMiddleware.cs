namespace Ecom.Gateway.Middleware;

/// <summary>
/// ZERO TRUST ARCHITECTURE: Gateway validates JWT as first line of defense.
/// Downstream services ALSO validate JWT independently (defense in depth).
/// This middleware forwards correlation/trace IDs only, NOT user context.
/// Services extract user info from JWT claims directly.
/// </summary>
public class UserContextMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<UserContextMiddleware> _logger;

    public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Forward correlation ID for distributed tracing
        if (!context.Request.Headers.ContainsKey(CorrelationIdHeaderName))
        {
            context.Request.Headers[CorrelationIdHeaderName] = context.TraceIdentifier;
        }

        // Log authentication status for monitoring
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value 
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            _logger.LogDebug("Authenticated request | UserId: {UserId} | TraceId: {TraceId}", 
                userId, context.TraceIdentifier);
        }

        await _next(context);
    }
}
