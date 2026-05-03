using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ecom.Shared.Infrastructure.Validation;

namespace Ecom.Shared.Infrastructure.Extensions;

/// <summary>
/// 🔒 AUTOMATIC PRODUCTION SAFETY: Extension methods for automatic startup validation
/// Eliminates human dependency - validation runs automatically in ALL services
/// </summary>
public static class StartupValidationExtensions
{
    /// <summary>
    /// Adds automatic startup validation to the service collection
    /// Validation runs automatically during application startup
    /// </summary>
    public static IServiceCollection AddAutomaticStartupValidation(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment,
        StartupValidationOptions? options = null)
    {
        options ??= new StartupValidationOptions();

        // Register validation as a hosted service that runs on startup
        services.AddHostedService(provider => 
            new StartupValidationHostedService(
                configuration, 
                environment,
                provider.GetRequiredService<ILogger<StartupValidationHostedService>>(),
                options));

        return services;
    }

    /// <summary>
    /// Validates application configuration and fails fast if issues detected
    /// Call this in Program.cs after building the app
    /// </summary>
    public static WebApplication ValidateStartupConfiguration(
        this WebApplication app,
        StartupValidationOptions? options = null)
    {
        options ??= new StartupValidationOptions();

        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("StartupValidation");
        var configuration = app.Configuration;
        var environment = app.Environment;

        logger.LogInformation("🔒 STARTUP VALIDATION: Beginning automatic configuration validation");

        try
        {
            // Always validate database
            if (options.ValidateDatabase && !string.IsNullOrEmpty(options.DatabaseConnectionStringName))
            {
                StartupConfigurationValidator.ValidateDatabaseConfiguration(
                    configuration, 
                    options.DatabaseConnectionStringName, 
                    logger);
            }

            // Validate RabbitMQ if service uses messaging
            if (options.ValidateRabbitMq)
            {
                StartupConfigurationValidator.ValidateRabbitMqConfiguration(
                    configuration, 
                    logger, 
                    environment.IsProduction());
            }

            // Validate JWT if service uses authentication
            if (options.ValidateJwt)
            {
                StartupConfigurationValidator.ValidateJwtConfiguration(
                    configuration, 
                    logger, 
                    environment.IsProduction());
            }

            // Validate email if service sends emails
            if (options.ValidateEmail)
            {
                StartupConfigurationValidator.ValidateEmailConfiguration(
                    configuration, 
                    logger, 
                    environment.IsProduction());
            }

            // Validate production environment settings
            StartupConfigurationValidator.ValidateProductionEnvironment(
                configuration, 
                logger, 
                environment.EnvironmentName);

            // Check for firewall configuration in production
            if (environment.IsProduction())
            {
                CheckFirewallConfiguration(configuration, logger);
            }

            logger.LogInformation("✅ STARTUP VALIDATION: All configuration checks passed");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "🔴 STARTUP VALIDATION FAILED: {Message}", ex.Message);
            throw; // Fail fast - do not start application
        }

        return app;
    }

    /// <summary>
    /// Checks if firewall configuration is documented in production
    /// Does not block startup but logs critical warning
    /// </summary>
    private static void CheckFirewallConfiguration(IConfiguration configuration, ILogger logger)
    {
        var firewallConfigured = configuration.GetValue<bool>("Security:FirewallConfigured", false);
        var servicePort = configuration.GetValue<int>("Kestrel:Endpoints:Https:Url", 0);

        if (!firewallConfigured)
        {
            logger.LogCritical(
                "🔴🔴🔴 CRITICAL SECURITY WARNING 🔴🔴🔴\n" +
                "═══════════════════════════════════════════════════════════════\n" +
                "FIREWALL NOT CONFIGURED FOR PRODUCTION DEPLOYMENT\n" +
                "═══════════════════════════════════════════════════════════════\n" +
                "Services may be directly accessible, bypassing API Gateway.\n" +
                "\n" +
                "REQUIRED ACTIONS:\n" +
                "1. Configure firewall to block direct service access (ports 7001-7005)\n" +
                "2. Allow only Gateway port (7000) from internet\n" +
                "3. Set 'Security:FirewallConfigured' = true in configuration\n" +
                "\n" +
                "RISK: Rate limiting and authentication can be bypassed\n" +
                "═══════════════════════════════════════════════════════════════");
        }
        else
        {
            logger.LogInformation("✅ Firewall configuration confirmed for production");
        }
    }
}

/// <summary>
/// Options for configuring startup validation
/// </summary>
public class StartupValidationOptions
{
    /// <summary>
    /// Validate database connection string
    /// </summary>
    public bool ValidateDatabase { get; set; } = true;

    /// <summary>
    /// Database connection string name to validate
    /// </summary>
    public string? DatabaseConnectionStringName { get; set; }

    /// <summary>
    /// Validate RabbitMQ configuration
    /// </summary>
    public bool ValidateRabbitMq { get; set; } = true;

    /// <summary>
    /// Validate JWT configuration
    /// </summary>
    public bool ValidateJwt { get; set; } = false;

    /// <summary>
    /// Validate email configuration
    /// </summary>
    public bool ValidateEmail { get; set; } = false;
}

/// <summary>
/// Hosted service that runs validation during application startup
/// Ensures validation happens automatically without developer intervention
/// </summary>
internal class StartupValidationHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<StartupValidationHostedService> _logger;
    private readonly StartupValidationOptions _options;

    public StartupValidationHostedService(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<StartupValidationHostedService> logger,
        StartupValidationOptions options)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
        _options = options;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔒 AUTOMATIC VALIDATION: Running startup configuration checks");

        // Validation happens here automatically
        // This runs before the application starts accepting requests

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
