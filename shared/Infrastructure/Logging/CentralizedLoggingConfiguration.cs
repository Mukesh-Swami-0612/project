using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace Ecom.Shared.Infrastructure.Logging;

/// <summary>
/// Centralized, asynchronous logging configuration for all services.
/// </summary>
public static class CentralizedLoggingConfiguration
{
    /// <summary>
    /// Configure centralized logging with async sinks and correlation tracking.
    /// </summary>
    public static LoggerConfiguration ConfigureCentralizedLogging(
        this LoggerConfiguration loggerConfig,
        IConfiguration configuration,
        string serviceName,
        IHostEnvironment? environment = null)
    {
        loggerConfig
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.WithCorrelationId()
            .Enrich.WithProperty("ServiceName", serviceName)
            .Enrich.WithProperty("Service", serviceName)
            .Enrich.WithProperty("Environment", environment?.EnvironmentName ?? "Unknown")
            .Enrich.WithProperty("Version", GetServiceVersion())
            .WriteTo.Async(a => a.Console(
                    formatter: new JsonFormatter(renderMessage: true)),
                blockWhenFull: false)
            .WriteTo.Async(a => a.Seq(
                    serverUrl: configuration["Logging:Seq:ServerUrl"] ?? "http://localhost:5341",
                    apiKey: configuration["Logging:Seq:ApiKey"],
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    bufferBaseFilename: $"./logs/{serviceName}-seq-buffer"),
                blockWhenFull: false)
            .WriteTo.Async(a => a.File(
                    path: $"./logs/{serviceName}-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    formatter: new JsonFormatter(),
                    restrictedToMinimumLevel: LogEventLevel.Information),
                blockWhenFull: false)
            .WriteTo.Async(a => a.Logger(lc => lc
                    .Filter.ByIncludingOnly(evt =>
                        evt.Level >= LogEventLevel.Warning ||
                        evt.Properties.ContainsKey("Performance") ||
                        evt.Properties.ContainsKey("Security") ||
                        evt.Properties.ContainsKey("Audit"))
                    .WriteTo.File(
                        path: $"./logs/{serviceName}-critical-.log",
                        rollingInterval: RollingInterval.Hour,
                        retainedFileCountLimit: 48,
                        formatter: new JsonFormatter())),
                blockWhenFull: false);

        var elasticsearchEnabled = bool.TryParse(configuration["Logging:Elasticsearch:Enabled"], out var enabled) && enabled;
        if (elasticsearchEnabled)
        {
            loggerConfig.WriteTo.Async(a => a.Elasticsearch(
                    nodeUris: configuration["Logging:Elasticsearch:Nodes"] ?? "http://localhost:9200",
                    indexFormat: $"ecom-logs-{serviceName.ToLowerInvariant()}-{{0:yyyy.MM.dd}}",
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    bufferBaseFilename: $"./logs/{serviceName}-elastic-buffer"),
                blockWhenFull: false);
        }

        var criticalLogsConnectionString = GetCriticalLogsConnectionString(configuration, serviceName);
        if (!string.IsNullOrWhiteSpace(criticalLogsConnectionString))
        {
            loggerConfig.WriteTo.Async(a => a.Logger(lc => lc
                    .Filter.ByIncludingOnly(IsCriticalDatabaseLog)
                    .WriteTo.MSSqlServer(
                        connectionString: criticalLogsConnectionString,
                        sinkOptions: new MSSqlServerSinkOptions
                        {
                            TableName = "CriticalLogs",
                            AutoCreateSqlTable = true,
                            BatchPostingLimit = 50
                        },
                        columnOptions: GetCriticalLogColumns())),
                blockWhenFull: false);
        }

        return loggerConfig;
    }

    /// <summary>
    /// Configure correlation ID middleware
    /// NOTE: Validation now runs via PreHostValidationGuard BEFORE builder.Build()
    /// NOTE: Middleware should NOT be registered in DI - they are activated via app.UseMiddleware<T>()
    /// </summary>
    public static IServiceCollection AddCorrelationIdLogging(this IServiceCollection services)
    {
        // Middleware should NOT be registered in DI container
        // They are activated via app.UseMiddleware<T>() in the pipeline
        // RequestDelegate cannot be resolved from DI - it's provided by the framework
        
        return services;
    }

    private static string? GetCriticalLogsConnectionString(IConfiguration configuration, string serviceName)
    {
        return configuration.GetConnectionString("LoggingDb")
            ?? configuration.GetConnectionString($"{serviceName}Db")
            ?? configuration.GetConnectionString("Default");
    }

    private static bool IsCriticalDatabaseLog(LogEvent evt)
    {
        return evt.Level >= LogEventLevel.Error ||
               evt.Properties.ContainsKey("Audit") ||
               evt.Properties.ContainsKey("Security");
    }

    /// <summary>
    /// Configure minimal columns for critical SQL logging.
    /// </summary>
    private static ColumnOptions GetCriticalLogColumns()
    {
        var columnOptions = new ColumnOptions();

        columnOptions.Store.Remove(StandardColumn.Properties);
        columnOptions.Store.Remove(StandardColumn.MessageTemplate);

        columnOptions.AdditionalColumns = new Collection<SqlColumn>
        {
            new() { ColumnName = "ServiceName", DataType = SqlDbType.NVarChar, DataLength = 50 },
            new() { ColumnName = "CorrelationId", DataType = SqlDbType.NVarChar, DataLength = 100 },
            new() { ColumnName = "UserId", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
            new() { ColumnName = "RequestPath", DataType = SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
            new() { ColumnName = "UserAgent", DataType = SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
            new() { ColumnName = "IpAddress", DataType = SqlDbType.NVarChar, DataLength = 45, AllowNull = true }
        };

        return columnOptions;
    }

    private static string GetServiceVersion()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            return assembly?.GetName().Version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    public static IDisposable BeginPerformanceScope(this Microsoft.Extensions.Logging.ILogger logger, string operation, object? parameters = null)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["Performance"] = true,
            ["Operation"] = operation,
            ["Parameters"] = parameters ?? new { },
            ["StartTime"] = DateTimeOffset.UtcNow
        }) ?? throw new InvalidOperationException("Failed to begin scope");
    }

    public static IDisposable BeginSecurityScope(this Microsoft.Extensions.Logging.ILogger logger, string action, string? userId = null)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["Security"] = true,
            ["Action"] = action,
            ["UserId"] = userId ?? "Anonymous",
            ["Timestamp"] = DateTimeOffset.UtcNow
        }) ?? throw new InvalidOperationException("Failed to begin scope");
    }

    public static IDisposable BeginAuditScope(this Microsoft.Extensions.Logging.ILogger logger, string entity, string action, string? userId = null)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["Audit"] = true,
            ["Entity"] = entity,
            ["Action"] = action,
            ["UserId"] = userId ?? "System",
            ["Timestamp"] = DateTimeOffset.UtcNow
        }) ?? throw new InvalidOperationException("Failed to begin scope");
    }
}

/// <summary>
/// DEPRECATED: Validation now runs via PreHostValidationGuard BEFORE builder.Build()
/// This class is kept for backward compatibility but is no longer used
/// </summary>
[Obsolete("Use PreHostValidationGuard.ValidateOrDie() before builder.Build() instead")]
internal class AutomaticStartupValidationService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Validation now happens in PreHostValidationGuard before host is built
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
