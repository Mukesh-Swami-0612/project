using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecom.Shared.Infrastructure.Validation;

/// <summary>
/// PRODUCTION SAFETY: Validates critical configuration at startup
/// Ensures application FAILS FAST if production credentials are not configured
/// </summary>
public static class StartupConfigurationValidator
{
    private static readonly string[] PlaceholderValues = 
    {
        "CHANGE_ME_IN_PRODUCTION",
        "REPLACE_WITH_ENV_VAR",
        "YOUR_APP_PASSWORD",
        "your-email@gmail.com"
    };

    /// <summary>
    /// Validates RabbitMQ configuration and fails if placeholders are detected
    /// </summary>
    public static void ValidateRabbitMqConfiguration(IConfiguration configuration, ILogger logger, bool isProduction = false)
    {
        var username = configuration["RabbitMq:Username"];
        var password = configuration["RabbitMq:Password"];
        var host = configuration["RabbitMq:Host"];
        var port = configuration["RabbitMq:Port"];

        if (string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "STARTUP FAILED: RabbitMQ password is not configured. " +
                "Set RabbitMq:Password in appsettings.Production.json or RABBITMQ_PASSWORD environment variable.");
        }

        if (ContainsPlaceholder(password))
        {
            throw new InvalidOperationException(
                $" STARTUP FAILED: RabbitMQ password contains placeholder value '{password}'. " +
                "Replace with actual credentials in production. " +
                "Set RABBITMQ_PASSWORD environment variable or update appsettings.Production.json");
        }

        // Strict fail-fast in production for default credentials
        if (password == "guest" && isProduction)
        {
            throw new InvalidOperationException(
                " STARTUP FAILED: RabbitMQ is using default 'guest' password in PRODUCTION. " +
                "This is a critical security risk. Change credentials immediately.");
        }
        else if (password == "guest")
        {
            logger.LogWarning(
                " SECURITY WARNING: RabbitMQ is using default 'guest' password. " +
                "This is acceptable for development but MUST be changed in production.");
        }

        logger.LogInformation("RabbitMQ configuration validated: {Host}:{Port} as {Username}", 
            host, port, username);
    }

    /// <summary>
    /// Validates JWT configuration and fails if key is missing or placeholder
    /// </summary>
    public static void ValidateJwtConfiguration(IConfiguration configuration, ILogger logger, bool isProduction = false)
    {
        var jwtKey = configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");

        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException(
                " STARTUP FAILED: JWT Key is not configured. " +
                "Set Jwt:Key in appsettings.Production.json or JWT_KEY environment variable.");
        }

        if (ContainsPlaceholder(jwtKey))
        {
            throw new InvalidOperationException(
                $" STARTUP FAILED: JWT Key contains placeholder value. " +
                "Replace with actual secret key in production. " +
                "Set JWT_KEY environment variable or update appsettings.Production.json");
        }

        if (jwtKey.Length < 32)
        {
            throw new InvalidOperationException(
                $" STARTUP FAILED: JWT Key is too short ({jwtKey.Length} characters). " +
                "Minimum 32 characters (256 bits) required for HS256 algorithm.");
        }

        logger.LogInformation(" JWT configuration validated: Key length = {Length} characters", jwtKey.Length);
    }

    /// <summary>
    /// Validates email configuration and fails if placeholders are detected in production
    /// </summary>
    public static void ValidateEmailConfiguration(IConfiguration configuration, ILogger logger, bool isProduction = false)
    {
        var email = configuration["EmailSettings:Email"] ?? configuration["Email:SmtpUser"];
        var password = configuration["EmailSettings:Password"] ?? configuration["Email:SmtpPassword"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (isProduction)
            {
                logger.LogWarning(
                    " Email configuration is incomplete in PRODUCTION. Email notifications will not work. " +
                    "Set EmailSettings:Email and EmailSettings:Password in configuration.");
            }
            else
            {
                logger.LogInformation("ℹ Email configuration is incomplete. Email notifications disabled.");
            }
            return; // Non-critical, allow startup
        }

        if (ContainsPlaceholder(email) || ContainsPlaceholder(password))
        {
            if (isProduction)
            {
                throw new InvalidOperationException(
                    " STARTUP FAILED: Email configuration contains placeholder values in PRODUCTION. " +
                    "Replace with actual credentials. Email notifications are required in production.");
            }
            else
            {
                logger.LogWarning(
                    " Email configuration contains placeholder values. Email notifications will not work.");
            }
            return;
        }

        logger.LogInformation(" Email configuration validated: {Email}", email);
    }

    /// <summary>
    /// Validates database connection string
    /// </summary>
    public static void ValidateDatabaseConfiguration(IConfiguration configuration, string connectionStringName, ILogger logger)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName);

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                $" STARTUP FAILED: Database connection string '{connectionStringName}' is not configured.");
        }

        logger.LogInformation(" Database configuration validated: {ConnectionStringName}", connectionStringName);
    }

    /// <summary>
    /// Validates production environment settings
    /// </summary>
    public static void ValidateProductionEnvironment(IConfiguration configuration, ILogger logger, string environmentName)
    {
        if (environmentName == "Production")
        {
            logger.LogInformation(" PRODUCTION MODE: Running in production environment");

            // Ensure HTTPS is enforced
            var requireHttps = configuration.GetValue<bool>("RequireHttpsMetadata", true);
            if (!requireHttps)
            {
                logger.LogWarning(" HTTPS metadata validation is disabled in production. This is a security risk.");
            }
        }
        else
        {
            logger.LogInformation(" DEVELOPMENT MODE: Running in {Environment} environment", environmentName);
        }
    }

    private static bool ContainsPlaceholder(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return PlaceholderValues.Any(placeholder => 
            value.Contains(placeholder, StringComparison.OrdinalIgnoreCase));
    }
}
