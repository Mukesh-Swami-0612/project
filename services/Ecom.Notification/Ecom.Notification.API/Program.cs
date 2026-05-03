using Ecom.Notification.Infrastructure.Persistence;
using Ecom.Notification.Infrastructure.Services;
using Ecom.Notification.Infrastructure.Repositories;
using Ecom.Notification.Domain.Interfaces;
using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Application.Services;
using Ecom.Notification.API.Swagger;
using Ecom.Notification.API.Middleware;
using Ecom.Shared.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Ecom.Shared.Infrastructure.Logging;

var bootstrapConfiguration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .Build();

// 🔒 ABSOLUTE VALIDATION LOCKDOWN - PRE-HOST ENFORCEMENT
// Runs BEFORE host is built - CANNOT be bypassed
var bootstrapEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var hostEnvironment = new BootstrapHostEnvironment(bootstrapEnvironment);
PreHostValidationGuard.ValidateOrDie(bootstrapConfiguration, hostEnvironment, "Notification");

var bootstrapNotificationDbConnection = bootstrapConfiguration.GetConnectionString("Default")
    ?? throw new Exception("Database not configured");

// ============================================
// STEP 1: Configure Serilog BEFORE building the app
// ============================================
Log.Logger = new LoggerConfiguration()
    .ConfigureCentralizedLogging(bootstrapConfiguration, "Notification")
    .CreateLogger();

try
{
    Log.Information("Starting Notification API...");

    var builder = WebApplication.CreateBuilder(args);
    var notificationDbConnection = builder.Configuration.GetConnectionString("Default")
        ?? throw new Exception("Database not configured");

    // ============================================
    // STEP 2: Use Serilog for logging
    // ============================================
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddMvc();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SchemaFilter<ScheduledAtIstSchemaFilter>();
    });

    // Database
    builder.Services.AddDbContext<NotificationDbContext>(options =>
        options.UseSqlServer(notificationDbConnection));

    // Services
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddSingleton<EmailTemplateService>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCorrelationIdLogging();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var authority = builder.Configuration["Jwt:Authority"]
                ?? throw new InvalidOperationException("Jwt:Authority is not configured.");
            var audience = builder.Configuration["Jwt:Audience"] ?? "ecom-clients";

            options.Authority = authority;
            options.Audience = audience;
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true
            };
        });

    builder.Services.AddAuthorization();

    // ── Health Checks ──────────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<NotificationDbContext>("database")
        .AddRabbitMQ(
            rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? "localhost"}:{builder.Configuration["RabbitMq:Port"] ?? "5672"}",
            name: "notification-rabbitmq",
            tags: new[] { "messaging", "rabbitmq" });

    // ── CORS ───────────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()); // 🔥 SECURITY: Enable credentials for HTTP-only cookies
    });

    var app = builder.Build();

    // ============================================
    // STEP 3: Add logging context middleware
    // ============================================
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseLoggingContext();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors("AllowAngular"); // 🔥 CORS: Enable cross-origin requests from Angular

    app.UseSerilogRequestLogging(); // Log HTTP requests

    app.UseAuthentication();
    app.UseMiddleware<UserLogContextMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Notification API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification API failed to start");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
