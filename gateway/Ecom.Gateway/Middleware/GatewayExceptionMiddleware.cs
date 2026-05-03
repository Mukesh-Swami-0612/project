using System.Text.Json;

namespace Ecom.Gateway.Middleware;

public class GatewayExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayExceptionMiddleware> _logger;

    public GatewayExceptionMiddleware(RequestDelegate next, ILogger<GatewayExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GATEWAY_ERROR | Path: {Path} | Method: {Method} | CorrelationId: {CorrelationId}",
                context.Request.Path,
                context.Request.Method,
                context.TraceIdentifier);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                error = "Internal Server Error",
                correlationId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
