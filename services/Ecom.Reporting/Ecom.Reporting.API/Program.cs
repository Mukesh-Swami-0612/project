using System.Text;
using Microsoft.OpenApi.Models;
using Ecom.Reporting.API.Middleware;
using Ecom.Reporting.Application.Configuration;
using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Application.Telemetry;
using Ecom.Reporting.Application.Mappings;
using Ecom.Reporting.Application.Services;
using Ecom.Reporting.Infrastructure.Persistence;
using Ecom.Reporting.Infrastructure.Repositories;
using Ecom.Reporting.Infrastructure.Messaging.Consumers;
using Ecom.Reporting.Infrastructure.BackgroundServices;
using Ecom.Reporting.Infrastructure.Services;
using Ecom.Shared.Infrastructure.Validation;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Ecom.Shared.Infrastructure.Logging;
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
PreHostValidationGuard.ValidateOrDie(bootstrapConfiguration, hostEnvironment, "Reporting");

var bootstrapReportingDbConnection = bootstrapConfiguration.GetConnectionString("ReportingDb")
    ?? throw new Exception("Database not configured");

// Configure Serilog with Centralized Logging
Log.Logger = new LoggerConfiguration()
    .ConfigureCentralizedLogging(bootstrapConfiguration, "Reporting")
    .CreateLogger();

try
{
    Log.Information("REPORTING_SERVICE_STARTING | Service initialization");

    var builder = WebApplication.CreateBuilder(args);

    var reportingDbConnection = builder.Configuration.GetConnectionString("ReportingDb")
        ?? throw new Exception("Database not configured");

    // Use Serilog
    builder.Host.UseSerilog();

builder.Services.AddDbContext<ReportingDbContext>(opt =>
    opt.UseSqlServer(reportingDbConnection));

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.Configure<IdempotencyOptions>(builder.Configuration.GetSection(IdempotencyOptions.SectionName));
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ReportingMappingProfile>());

// RabbitMQ Consumers
builder.Services.AddHostedService<InventoryUpdatedConsumer>();
builder.Services.AddHostedService<PricingUpdatedConsumer>();
builder.Services.AddHostedService<ProductApprovedConsumer>();
builder.Services.AddHostedService<ProductPublishedConsumer>();
builder.Services.AddHostedService<NotificationSentConsumer>();
builder.Services.AddHostedService<NotificationFailedConsumer>();
builder.Services.AddHostedService<WorkflowCompletedConsumer>();
builder.Services.AddHostedService<WorkflowFailedConsumer>();
builder.Services.AddHostedService<ProductStatusChangedConsumer>();
builder.Services.AddHostedService<ProductRejectedConsumer>();
builder.Services.AddHostedService<UserActionConsumer>();

// Background Services
builder.Services.AddHostedService<ReportingLogCleanupService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdLogging();

// ── OpenTelemetry Distributed Tracing ─────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Ecom.Reporting", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddSource(ReportingActivitySource.SourceName)
        .AddConsoleExporter());

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: reportingDbConnection,
        name: "database",
        tags: new[] { "db", "sql" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? "localhost"}:{builder.Configuration["RabbitMq:Port"] ?? "5672"}",
        name: "reporting-rabbitmq",
        tags: new[] { "messaging", "rabbitmq" });

// ── JWT Authentication (Authority-based) ───────────────────────────────────────
// Reporting Service validates tokens issued by Auth Service (no shared secret)
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
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── CORS ───────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://localhost:4200",
                "http://localhost:62295"  // Add the actual port Angular is using
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // 🔥 SECURITY: Enable credentials for HTTP-only cookies
});

builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ReportingMappingProfile>();
builder.Services.AddFluentValidationAutoValidation();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token in the format: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ReportingLoggingMiddleware>();
app.UseCors("AllowAngular"); // 🔥 CORS: Enable cross-origin requests from Angular
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseMiddleware<UserLogContextMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "REPORTING_SERVICE_FAILED | Service failed to start");
}
finally
{
    Log.Information("REPORTING_SERVICE_SHUTDOWN | Service shutting down");
    Log.CloseAndFlush();
}
