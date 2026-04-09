namespace Ecom.Gateway.Middleware;

public class GatewayLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayLoggingMiddleware> _logger;

    public GatewayLoggingMiddleware(RequestDelegate next, ILogger<GatewayLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();
        context.Request.Headers["X-Correlation-Id"] = correlationId;

        _logger.LogInformation("Gateway request {Method} {Path} | CorrelationId: {CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);

        await _next(context);

        _logger.LogInformation("Gateway response {StatusCode} | CorrelationId: {CorrelationId}",
            context.Response.StatusCode, correlationId);
    }
}
