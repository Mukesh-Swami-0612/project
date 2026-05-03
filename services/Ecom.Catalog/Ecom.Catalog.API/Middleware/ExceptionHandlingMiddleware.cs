using System.Net;
using System.Text.Json;
using Ecom.Catalog.Application.Exceptions;
using Ecom.Catalog.Domain.Exceptions;

namespace Ecom.Catalog.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (DomainException ex)
        {
            // 🔥 DOMAIN VALIDATION ERROR: Return 400 Bad Request with validation details
            _logger.LogWarning(ex, "Domain validation failed: {Message}", ex.Message);
            await WriteAsync(context, HttpStatusCode.BadRequest, new
            {
                error = ex.Message,
                errorCode = "DOMAIN_VALIDATION_ERROR",
                propertyName = ex.PropertyName
            });
        }
        catch (ConcurrencyException ex)
        {
            // 🔥 CONCURRENCY CONFLICT: Return 409 Conflict with clear message
            _logger.LogWarning(ex, "Concurrency conflict for {EntityType} {EntityId}", ex.EntityType, ex.EntityId);
            await WriteAsync(context, HttpStatusCode.Conflict, new
            {
                error = ex.Message,
                errorCode = "CONCURRENCY_CONFLICT",
                entityType = ex.EntityType,
                entityId = ex.EntityId
            });
        }
        catch (KeyNotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." });
        }
    }

    private static Task WriteAsync(HttpContext ctx, HttpStatusCode code, object response)
    {
        ctx.Response.StatusCode = (int)code;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
