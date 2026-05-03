using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Ecom.Shared.Infrastructure.Logging;

/// <summary>
/// 🔥 PERFORMANCE: Monitor and log performance metrics with correlation tracking
/// </summary>
public class LoggingPerformanceMonitor : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;
    private readonly Dictionary<string, object> _context;
    private bool _disposed;

    public LoggingPerformanceMonitor(ILogger logger, string operationName, object? parameters = null)
    {
        _logger = logger;
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
        _context = new Dictionary<string, object>
        {
            ["Operation"] = operationName,
            ["StartTime"] = DateTimeOffset.UtcNow,
            ["Parameters"] = parameters ?? new { }
        };

        _logger.LogDebug("🔥 PERFORMANCE: Started operation {Operation}", operationName);
    }

    /// <summary>
    /// Add context information to the performance log
    /// </summary>
    public LoggingPerformanceMonitor WithContext(string key, object value)
    {
        _context[key] = value;
        return this;
    }

    /// <summary>
    /// Add multiple context properties
    /// </summary>
    public LoggingPerformanceMonitor WithContext(Dictionary<string, object> context)
    {
        foreach (var kvp in context)
        {
            _context[kvp.Key] = kvp.Value;
        }
        return this;
    }

    /// <summary>
    /// Log intermediate checkpoint
    /// </summary>
    public void Checkpoint(string checkpointName, object? data = null)
    {
        var elapsed = _stopwatch.ElapsedMilliseconds;
        
        _logger.LogInformation("🔥 PERFORMANCE: Checkpoint {Checkpoint} in operation {Operation} at {ElapsedMs}ms",
            checkpointName, _operationName, elapsed);

        if (data != null)
        {
            _context[$"Checkpoint_{checkpointName}"] = data;
        }
    }

    /// <summary>
    /// Complete operation and log final metrics
    /// </summary>
    public void Complete(bool success = true, string? errorMessage = null)
    {
        if (_disposed) return;

        _stopwatch.Stop();
        var elapsed = _stopwatch.ElapsedMilliseconds;

        _context["EndTime"] = DateTimeOffset.UtcNow;
        _context["ElapsedMs"] = elapsed;
        _context["Success"] = success;

        if (!string.IsNullOrEmpty(errorMessage))
        {
            _context["ErrorMessage"] = errorMessage;
        }

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;
        var status = success ? "✅ COMPLETED" : "❌ FAILED";

        _logger.Log(logLevel, 
            "🔥 PERFORMANCE: {Status} operation {Operation} in {ElapsedMs}ms | Context: {@Context}",
            status, _operationName, elapsed, _context);

        // Log performance warning for slow operations
        if (elapsed > 5000) // 5 seconds
        {
            _logger.LogWarning("🐌 SLOW OPERATION: {Operation} took {ElapsedMs}ms - consider optimization",
                _operationName, elapsed);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Complete();
            _disposed = true;
        }
    }
}

/// <summary>
/// Extension methods for easy performance monitoring
/// </summary>
public static class PerformanceMonitoringExtensions
{
    /// <summary>
    /// Start performance monitoring for an operation
    /// </summary>
    public static LoggingPerformanceMonitor StartPerformanceMonitor(this ILogger logger, string operationName, object? parameters = null)
    {
        return new LoggingPerformanceMonitor(logger, operationName, parameters);
    }

    /// <summary>
    /// Monitor async operation performance
    /// </summary>
    public static async Task<T> MonitorPerformanceAsync<T>(this ILogger logger, string operationName, Func<Task<T>> operation, object? parameters = null)
    {
        using var monitor = logger.StartPerformanceMonitor(operationName, parameters);
        
        try
        {
            var result = await operation();
            monitor.Complete(success: true);
            return result;
        }
        catch (Exception ex)
        {
            monitor.Complete(success: false, errorMessage: ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Monitor sync operation performance
    /// </summary>
    public static T MonitorPerformance<T>(this ILogger logger, string operationName, Func<T> operation, object? parameters = null)
    {
        using var monitor = logger.StartPerformanceMonitor(operationName, parameters);
        
        try
        {
            var result = operation();
            monitor.Complete(success: true);
            return result;
        }
        catch (Exception ex)
        {
            monitor.Complete(success: false, errorMessage: ex.Message);
            throw;
        }
    }
}