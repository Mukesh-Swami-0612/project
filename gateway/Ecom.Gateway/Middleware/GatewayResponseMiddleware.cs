using System.Text;
using System.Text.Json;

namespace Ecom.Gateway.Middleware;

/// <summary>
/// Standardizes all API responses to consistent format
/// Wraps downstream responses in standard envelope
/// </summary>
public class GatewayResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayResponseMiddleware> _logger;

    public GatewayResponseMiddleware(RequestDelegate next, ILogger<GatewayResponseMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip wrapping for certain paths
        if (ShouldSkipWrapping(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;

        try
        {
            using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            await _next(context);

            newBody.Seek(0, SeekOrigin.Begin);
            var bodyText = await new StreamReader(newBody).ReadToEndAsync();

            context.Response.Body = originalBody;

            // Determine if response is successful
            var isSuccess = context.Response.StatusCode >= 200 && context.Response.StatusCode < 400;

            // Parse existing response if it's JSON
            object? data = null;
            object? error = null;

            if (!string.IsNullOrEmpty(bodyText))
            {
                try
                {
                    if (isSuccess)
                    {
                        data = JsonSerializer.Deserialize<object>(bodyText);
                    }
                    else
                    {
                        error = JsonSerializer.Deserialize<object>(bodyText);
                    }
                }
                catch
                {
                    // If not JSON, use as-is
                    if (isSuccess)
                        data = bodyText;
                    else
                        error = bodyText;
                }
            }

            // Create standardized response
            var wrappedResponse = new
            {
                success = isSuccess,
                data = data,
                error = error,
                statusCode = context.Response.StatusCode,
                correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? context.TraceIdentifier,
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(wrappedResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.ContentType = "application/json";
            context.Response.ContentLength = Encoding.UTF8.GetByteCount(jsonResponse);
            await context.Response.WriteAsync(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in response wrapping middleware");
            context.Response.Body = originalBody;
            throw;
        }
    }

    private bool ShouldSkipWrapping(PathString path)
    {
        // Skip wrapping for these paths
        var skipPaths = new[]
        {
            "/health",
            "/swagger",
            "/favicon.ico",
            "/api/v1/auth",
            "/gateway/auth"
        };

        return skipPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}
