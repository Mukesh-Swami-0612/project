using Ecom.Notification.Infrastructure.Persistence;
using Ecom.Notification.Domain.Interfaces;
using Ecom.Notification.Infrastructure.Services;
using Ecom.Notification.Infrastructure.Repositories;
using Ecom.Notification.Application.Configuration;
using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Application.Services;
using Ecom.Notification.Application.Telemetry;
using Ecom.Notification.Infrastructure.Messaging;
using Ecom.Notification.Infrastructure.Messaging.Consumers;
using Ecom.Notification.Worker;
using Microsoft.EntityFrameworkCore;
using Ecom.Shared.Infrastructure.Services;
using Ecom.Shared.Infrastructure.Logging;
using Ecom.Shared.Infrastructure.Validation;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var bootstrapConfiguration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// 🔒 ABSOLUTE VALIDATION LOCKDOWN - PRE-HOST ENFORCEMENT
// Runs BEFORE host is built - CANNOT be bypassed
var bootstrapEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var hostEnvironment = new BootstrapHostEnvironment(bootstrapEnvironment);
PreHostValidationGuard.ValidateOrDie(bootstrapConfiguration, hostEnvironment, "NotificationWorker");

// ============================================
// STEP 1: Configure Serilog BEFORE building the host
// ============================================
Log.Logger = new LoggerConfiguration()
    .ConfigureCentralizedLogging(bootstrapConfiguration, "NotificationWorker")
    .CreateLogger();

try
{
    Log.Information("Starting Notification Worker...");

    var builder = Host.CreateApplicationBuilder(args);

    // ============================================
    // STEP 2: Use Serilog for logging
    // ============================================
    builder.Services.AddSerilog();

    // 1. Database
    var notificationDbConnection = builder.Configuration.GetConnectionString("Default")
        ?? throw new Exception("Database not configured");

    builder.Services.AddDbContext<NotificationDbContext>(options =>
        options.UseSqlServer(notificationDbConnection));

    // 2. Services & Repositories
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
    builder.Services.Configure<IdempotencyOptions>(builder.Configuration.GetSection(IdempotencyOptions.SectionName));
    builder.Services.AddSingleton<EmailTemplateService>();

    // ── OpenTelemetry Distributed Tracing ─────────────────────────────────────────
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService("Ecom.Notification", serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddHttpClientInstrumentation()
            .AddSource(NotificationActivitySource.SourceName)
            .AddConsoleExporter());

    // 3. Event Consumers
    builder.Services.AddSingleton<ProductApprovedConsumer>();
    builder.Services.AddSingleton<ProductRejectedConsumer>();
    builder.Services.AddSingleton<ProductPublishedConsumer>();
    builder.Services.AddSingleton<WorkflowFailedConsumer>();
    builder.Services.AddSingleton<UserRegisteredConsumer>();
    builder.Services.AddSingleton<UserLoginSuccessConsumer>();

    // 4. Background Services
    builder.Services.AddHostedService<RabbitMqConsumerService>();
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddHostedService<LogCleanupService>();
    builder.Services.AddHostedService<AlertMonitoringService>();

    var host = builder.Build();
    
    Log.Information("Notification Worker started successfully");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification Worker failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
