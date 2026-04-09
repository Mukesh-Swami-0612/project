using System.Net;
using System.Text.Json;
using Ecom.Auth.Application.Exceptions;
using Microsoft.Data.SqlClient;

namespace Ecom.Auth.API.Middleware;

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
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            // Custom application exceptions with status codes
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            
            _logger.LogWarning(
                "Application exception: {Message} | StatusCode={StatusCode} | CorrelationId={CorrelationId} | Path={Path}",
                ex.Message, ex.StatusCode, correlationId, context.Request.Path);

            if (ex is ValidationException validationEx)
            {
                await WriteValidationErrorAsync(context, validationEx, correlationId);
            }
            else
            {
                await WriteErrorAsync(context, ex.StatusCode, ex.Message, correlationId);
            }
        }
        catch (SqlException ex)
        {
            // Database connection failures
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            
            _logger.LogError(ex,
                "Database error: {Message} | CorrelationId={CorrelationId} | Path={Path}",
                ex.Message, correlationId, context.Request.Path);

            await WriteErrorAsync(context, 503, 
                "Database connection failed. Please try again later.", correlationId);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Legacy exception support (will be replaced with UnauthorizedException)
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            
            _logger.LogWarning(
                "Unauthorized access: {Message} | CorrelationId={CorrelationId} | Path={Path}",
                ex.Message, correlationId, context.Request.Path);

            await WriteErrorAsync(context, 401, ex.Message, correlationId);
        }
        catch (InvalidOperationException ex)
        {
            // Legacy exception support (will be replaced with ConflictException)
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            
            _logger.LogWarning(
                "Invalid operation: {Message} | CorrelationId={CorrelationId} | Path={Path}",
                ex.Message, correlationId, context.Request.Path);

            await WriteErrorAsync(context, 409, ex.Message, correlationId);
        }
        catch (Exception ex)
        {
            // Unhandled exceptions
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            
            _logger.LogError(ex,
                "Unhandled exception: {Message} | CorrelationId={CorrelationId} | Path={Path}",
                ex.Message, correlationId, context.Request.Path);

            await WriteErrorAsync(context, 500,
                "An unexpected error occurred. Please try again later.", correlationId);
        }
    }

    private static Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        string correlationId)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = statusCode,
            message,
            correlationId
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }

    private static Task WriteValidationErrorAsync(
        HttpContext context,
        ValidationException ex,
        string correlationId)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = 400,
            message = ex.Message,
            errors = ex.Errors,
            correlationId
        };

        var json = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(json);
    }
}
