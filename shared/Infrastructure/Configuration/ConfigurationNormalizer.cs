using Microsoft.Extensions.Configuration;

namespace Ecom.Shared.Infrastructure.Configuration;

/// <summary>
/// 🔧 CONFIGURATION NORMALIZATION: Standardizes configuration keys across services
/// Handles inconsistencies like RabbitMq vs RabbitMQ
/// </summary>
public static class ConfigurationNormalizer
{
    /// <summary>
    /// Gets RabbitMQ configuration value with automatic fallback to alternative naming
    /// Standardizes on "RabbitMq" (capital M, lowercase q)
    /// </summary>
    public static string? GetRabbitMqValue(this IConfiguration configuration, string key)
    {
        // Standard: RabbitMq (capital M, lowercase q)
        var standardValue = configuration[$"RabbitMq:{key}"];
        if (!string.IsNullOrEmpty(standardValue))
            return standardValue;

        // Fallback: RabbitMQ (both capitals)
        var fallbackValue = configuration[$"RabbitMQ:{key}"];
        if (!string.IsNullOrEmpty(fallbackValue))
            return fallbackValue;

        // Environment variable fallback
        var envKey = $"RABBITMQ_{key.ToUpperInvariant()}";
        return Environment.GetEnvironmentVariable(envKey);
    }

    /// <summary>
    /// Gets RabbitMQ configuration with all standard keys
    /// </summary>
    public static RabbitMqConfiguration GetRabbitMqConfiguration(this IConfiguration configuration)
    {
        return new RabbitMqConfiguration
        {
            Host = configuration.GetRabbitMqValue("Host") ?? "localhost",
            Port = int.Parse(configuration.GetRabbitMqValue("Port") ?? "5672"),
            Username = configuration.GetRabbitMqValue("Username") ?? "guest",
            Password = configuration.GetRabbitMqValue("Password") ?? "guest",
            VirtualHost = configuration.GetRabbitMqValue("VirtualHost") ?? "/",
            Exchange = configuration.GetRabbitMqValue("Exchange") ?? "ecom-events"
        };
    }

    /// <summary>
    /// Builds RabbitMQ connection string with normalized configuration
    /// </summary>
    public static string GetRabbitMqConnectionString(this IConfiguration configuration)
    {
        var config = configuration.GetRabbitMqConfiguration();
        return $"amqp://{config.Username}:{config.Password}@{config.Host}:{config.Port}{config.VirtualHost}";
    }
}

/// <summary>
/// Normalized RabbitMQ configuration
/// </summary>
public class RabbitMqConfiguration
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "ecom-events";

    /// <summary>
    /// Gets AMQP connection string
    /// </summary>
    public string GetConnectionString()
    {
        return $"amqp://{Username}:{Password}@{Host}:{Port}{VirtualHost}";
    }
}
