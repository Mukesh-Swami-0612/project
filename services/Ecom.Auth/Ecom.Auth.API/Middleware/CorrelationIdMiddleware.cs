namespace Ecom.Auth.API.Middleware;

public class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Use incoming header if present, otherwise generate a new one
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId)
            || string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        context.Items["CorrelationId"] = correlationId.ToString();
        context.Response.Headers[HeaderName] = correlationId.ToString();

        await _next(context);
    }
}
