using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ecom.Shared.Infrastructure.Validation;

/// <summary>
/// 🔒 ABSOLUTE VALIDATION LOCKDOWN - PRE-HOST ENFORCEMENT
/// Executes BEFORE builder.Build() - CANNOT be bypassed
/// Zero dependency on logging, DI, or any infrastructure
/// Fails with Environment.Exit(1) if validation fails
/// </summary>
public static class PreHostValidationGuard
{
    private static readonly string[] PlaceholderValues = 
    {
        "CHANGE_ME_IN_PRODUCTION",
        "REPLACE_WITH_ENV_VAR",
        "YOUR_APP_PASSWORD",
        "your-email@gmail.com"
    };

    /// <summary>
    /// MANDATORY PRE-HOST VALIDATION
    /// Call this BEFORE builder.Build() in Program.cs
    /// Validates ALL critical configuration and exits if invalid
    /// </summary>
    public static void ValidateOrDie(IConfiguration configuration, IHostEnvironment environment, string serviceName)
    {
        Console.WriteLine($"🔒 PRE-HOST VALIDATION GUARD: Starting validation for {serviceName}");
        Console.WriteLine($"   Environment: {environment.EnvironmentName}");
        Console.WriteLine($"   Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();

        var isProduction = environment.IsProduction();
        var validationFailed = false;
        var errors = new List<string>();

        try
        {
            // 1. MANDATORY: Database validation
            ValidateDatabaseOrDie(configuration, serviceName, errors);

            // 2. MANDATORY: RabbitMQ validation (if configured)
            ValidateRabbitMqOrDie(configuration, isProduction, errors);

            // 3. MANDATORY: JWT validation (if configured)
            ValidateJwtOrDie(configuration, isProduction, errors);

            // 4. MANDATORY: Email validation (if configured)
            ValidateEmailOrDie(configuration, isProduction, errors);

            // 5. MANDATORY: Production environment checks
            ValidateProductionEnvironmentOrDie(configuration, environment, errors);

            // 6. MANDATORY: Firewall check in production
            if (isProduction)
            {
                ValidateFirewallOrDie(configuration, errors);
            }

            if (errors.Any())
            {
                validationFailed = true;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"UNEXPECTED VALIDATION ERROR: {ex.Message}");
            validationFailed = true;
        }

        if (validationFailed)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("🔴🔴🔴 PRE-HOST VALIDATION FAILED 🔴🔴🔴");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"Service: {serviceName}");
            Console.WriteLine($"Environment: {environment.EnvironmentName}");
            Console.WriteLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine();
            Console.WriteLine("VALIDATION ERRORS:");
            for (int i = 0; i < errors.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {errors[i]}");
            }
            Console.WriteLine();
            Console.WriteLine("APPLICATION STARTUP ABORTED");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();

            // ABSOLUTE LOCKDOWN: Exit immediately
            Environment.Exit(1);
        }

        Console.WriteLine("✅ PRE-HOST VALIDATION: All checks passed");
        Console.WriteLine($"   Service {serviceName} is cleared for startup");
        Console.WriteLine();
    }

    /// <summary>
    /// MANDATORY: Validate database configuration
    /// </summary>
    private static void ValidateDatabaseOrDie(IConfiguration configuration, string serviceName, List<string> errors)
    {
        // Try common connection string names based on service
        var connectionStringNames = new[] 
        { 
            $"{serviceName}Db",
            "AuthDb", 
            "CatalogDb", 
            "WorkflowDb", 
            "ReportingDb",
            "NotificationDb",
            "Default" 
        };

        string? foundConnectionString = null;
        string? foundName = null;

        foreach (var name in connectionStringNames)
        {
            var connectionString = configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(connectionString))
            {
                foundConnectionString = connectionString;
                foundName = name;
                break;
            }
        }

        if (string.IsNullOrEmpty(foundConnectionString))
        {
            errors.Add($"DATABASE: No connection string found. Checked: {string.Join(", ", connectionStringNames)}");
            return;
        }

        Console.WriteLine($"✓ DATABASE: Connection string '{foundName}' validated");
    }

    /// <summary>
    /// MANDATORY: Validate RabbitMQ configuration if present
    /// </summary>
    private static void ValidateRabbitMqOrDie(IConfiguration configuration, bool isProduction, List<string> errors)
    {
        var host = configuration["RabbitMq:Host"];
        var port = configuration["RabbitMq:Port"];
        var username = configuration["RabbitMq:Username"];
        var password = configuration["RabbitMq:Password"];

        // Check if RabbitMQ is configured at all
        if (string.IsNullOrEmpty(host) && string.IsNullOrEmpty(password))
        {
            Console.WriteLine("⊘ RABBITMQ: Not configured, skipping validation");
            return;
        }

        // If ANY RabbitMQ config exists, ALL must be valid
        if (string.IsNullOrEmpty(password))
        {
            errors.Add("RABBITMQ: Password is not configured. Set RabbitMq:Password or RABBITMQ_PASSWORD environment variable");
            return;
        }

        if (ContainsPlaceholder(password))
        {
            errors.Add($"RABBITMQ: Password contains placeholder value '{password}'. Replace with actual credentials");
            return;
        }

        // Strict fail-fast in production for default credentials
        if (password == "guest" && isProduction)
        {
            errors.Add("RABBITMQ: Using default 'guest' password in PRODUCTION. This is a critical security risk");
            return;
        }
        else if (password == "guest")
        {
            Console.WriteLine("⚠ RABBITMQ: Using default 'guest' password (acceptable for development only)");
        }

        Console.WriteLine($"✓ RABBITMQ: Configuration validated ({host}:{port} as {username})");
    }

    /// <summary>
    /// MANDATORY: Validate JWT configuration if present
    /// </summary>
    private static void ValidateJwtOrDie(IConfiguration configuration, bool isProduction, List<string> errors)
    {
        var jwtKey = configuration["Jwt:Key"] ?? Environment.GetEnvironmentVariable("JWT_KEY");
        var jwtAuthority = configuration["Jwt:Authority"];

        // Check if JWT is configured at all
        if (string.IsNullOrEmpty(jwtKey) && string.IsNullOrEmpty(jwtAuthority))
        {
            Console.WriteLine("⊘ JWT: Not configured, skipping validation");
            return;
        }

        // If Authority is set, this service validates tokens (no key needed)
        if (!string.IsNullOrEmpty(jwtAuthority))
        {
            Console.WriteLine($"✓ JWT: Authority-based validation configured ({jwtAuthority})");
            return;
        }

        // If no Authority, then Key is required (this service issues tokens)
        if (string.IsNullOrEmpty(jwtKey))
        {
            errors.Add("JWT: Key is not configured. Set Jwt:Key or JWT_KEY environment variable");
            return;
        }

        if (ContainsPlaceholder(jwtKey))
        {
            errors.Add("JWT: Key contains placeholder value. Replace with actual secret key");
            return;
        }

        if (jwtKey.Length < 32)
        {
            errors.Add($"JWT: Key is too short ({jwtKey.Length} characters). Minimum 32 characters (256 bits) required for HS256");
            return;
        }

        Console.WriteLine($"✓ JWT: Key validated (length: {jwtKey.Length} characters)");
    }

    /// <summary>
    /// MANDATORY: Validate email configuration if present
    /// </summary>
    private static void ValidateEmailOrDie(IConfiguration configuration, bool isProduction, List<string> errors)
    {
        var email = configuration["EmailSettings:Email"] ?? configuration["Email:SmtpUser"];
        var password = configuration["EmailSettings:Password"] ?? configuration["Email:SmtpPassword"];

        // Check if Email is configured at all
        if (string.IsNullOrEmpty(email) && string.IsNullOrEmpty(password))
        {
            Console.WriteLine("⊘ EMAIL: Not configured, skipping validation");
            return;
        }

        // If ANY email config exists, validate it
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            if (isProduction)
            {
                errors.Add("EMAIL: Configuration is incomplete in PRODUCTION. Email notifications are required");
            }
            else
            {
                Console.WriteLine("⚠ EMAIL: Configuration is incomplete (email notifications disabled)");
            }
            return;
        }

        if (ContainsPlaceholder(email) || ContainsPlaceholder(password))
        {
            if (isProduction)
            {
                errors.Add("EMAIL: Configuration contains placeholder values in PRODUCTION");
            }
            else
            {
                Console.WriteLine("⚠ EMAIL: Configuration contains placeholder values");
            }
            return;
        }

        Console.WriteLine($"✓ EMAIL: Configuration validated ({email})");
    }

    /// <summary>
    /// MANDATORY: Validate production environment settings
    /// </summary>
    private static void ValidateProductionEnvironmentOrDie(IConfiguration configuration, IHostEnvironment environment, List<string> errors)
    {
        if (environment.IsProduction())
        {
            Console.WriteLine("✓ ENVIRONMENT: Production mode active");

            // Ensure HTTPS is enforced
            var requireHttps = configuration.GetValue<bool>("RequireHttpsMetadata", true);
            if (!requireHttps)
            {
                Console.WriteLine("⚠ ENVIRONMENT: HTTPS metadata validation is disabled (security risk)");
            }
        }
        else
        {
            Console.WriteLine($"✓ ENVIRONMENT: Development mode ({environment.EnvironmentName})");
        }
    }

    /// <summary>
    /// MANDATORY: Validate firewall configuration in production
    /// </summary>
    private static void ValidateFirewallOrDie(IConfiguration configuration, List<string> errors)
    {
        var firewallConfigured = configuration.GetValue<bool>("Security:FirewallConfigured", false);

        if (!firewallConfigured)
        {
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("🔴🔴🔴 CRITICAL SECURITY WARNING 🔴🔴🔴");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("FIREWALL NOT CONFIGURED FOR PRODUCTION DEPLOYMENT");
            Console.WriteLine();
            Console.WriteLine("Services may be directly accessible, bypassing API Gateway.");
            Console.WriteLine();
            Console.WriteLine("REQUIRED ACTIONS:");
            Console.WriteLine("1. Configure firewall to block direct service access (ports 7001-7005)");
            Console.WriteLine("2. Allow only Gateway port (7000) from internet");
            Console.WriteLine("3. Set 'Security:FirewallConfigured' = true in configuration");
            Console.WriteLine();
            Console.WriteLine("RISK: Rate limiting and authentication can be bypassed");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();

            errors.Add("FIREWALL: Not configured for production deployment");
        }
        else
        {
            Console.WriteLine("✓ FIREWALL: Configuration confirmed for production");
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
