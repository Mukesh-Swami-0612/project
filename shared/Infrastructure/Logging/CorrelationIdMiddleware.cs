using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Ecom.Shared.Infrastructure.Logging;

/// <summary>
/// 🔥 CORRELATION: Middleware to ensure correlation ID tracking across all requests
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdKey = "CorrelationId";

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        // Normalize the header so Ocelot/downstream clients forward the same ID.
        context.Request.Headers[CorrelationIdHeaderName] = correlationId;
        
        // Add to response headers for client tracking
        context.Response.Headers.TryAdd(CorrelationIdHeaderName, correlationId);
        
        // Add to HttpContext for downstream access
        context.Items[CorrelationIdKey] = correlationId;
        
        // Add to Serilog context for all logs in this request
        using (LogContext.PushProperty(CorrelationIdKey, correlationId))
        using (LogContext.PushProperty("RequestPath", context.Request.Path))
        using (LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (LogContext.PushProperty("UserAgent", context.Request.Headers["User-Agent"].FirstOrDefault()))
        using (LogContext.PushProperty("IpAddress", GetClientIpAddress(context)))
        {
            // Extract user ID if available
            var userId = context.User?.Identity?.Name ?? 
                        context.User?.FindFirst("sub")?.Value ?? 
                        context.User?.FindFirst("userId")?.Value;
            
            if (!string.IsNullOrEmpty(userId))
            {
                using (LogContext.PushProperty("UserId", userId))
                {
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }

    /// <summary>
    /// Get existing correlation ID from headers or create new one
    /// </summary>
    private string GetOrCreateCorrelationId(HttpContext context)
    {
        // Check if correlation ID exists in request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var correlationId) &&
            !string.IsNullOrEmpty(correlationId))
        {
            return correlationId.ToString();
        }

        // Check alternative header names
        var alternativeHeaders = new[] { "X-Request-ID", "X-Trace-ID", "Request-ID" };
        foreach (var header in alternativeHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var altCorrelationId) &&
                !string.IsNullOrEmpty(altCorrelationId))
            {
                return altCorrelationId.ToString();
            }
        }

        // Generate new correlation ID
        var newCorrelationId = Guid.NewGuid().ToString("N")[..12]; // Short format for readability
        
        _logger.LogDebug("Generated new correlation ID: {CorrelationId} for {RequestPath}", 
            newCorrelationId, context.Request.Path);
        
        return newCorrelationId;
    }

    /// <summary>
    /// Extract client IP address considering proxies
    /// </summary>
    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP if multiple are present
            return forwardedFor.Split(',')[0].Trim();
        }

        // Check for real IP header
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // Fallback to connection remote IP
        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}

/// <summary>
/// Extension methods for correlation ID access
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Get correlation ID from HttpContext
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items[CorrelationIdMiddleware.CorrelationIdKey]?.ToString();
    }

    /// <summary>
    /// Get correlation ID from HttpContextAccessor
    /// </summary>
    public static string? GetCorrelationId(this IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.GetCorrelationId();
    }
}
