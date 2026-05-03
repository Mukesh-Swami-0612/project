using System.Text;
using System.Threading.RateLimiting;
using Ecom.Auth.API.Middleware;
using Ecom.Auth.API.BackgroundServices;
using Ecom.Auth.Application.Interfaces;
using Ecom.Auth.Application.Mappings;
using Ecom.Auth.Application.Services;
using Ecom.Auth.Application.Telemetry;
using Ecom.Auth.Infrastructure.Persistence;
using Ecom.Auth.Infrastructure.Repositories;
using Ecom.Shared.Contracts.Interfaces;
using Ecom.Shared.Infrastructure.Messaging;
using Ecom.Shared.Infrastructure.Validation;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ecom.Shared.Infrastructure.Logging;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedCorrelationIdMiddleware = Ecom.Shared.Infrastructure.Logging.CorrelationIdMiddleware;

var builder = WebApplication.CreateBuilder(args);

// Ensure critical secrets are read from Environment Variables (for security)
var envJwtKey = Environment.GetEnvironmentVariable("JWT_KEY");
if (!string.IsNullOrEmpty(envJwtKey))
{
    builder.Configuration["Jwt:Key"] = envJwtKey;
}

// 🔒 ABSOLUTE VALIDATION LOCKDOWN - PRE-HOST ENFORCEMENT
// Runs BEFORE builder.Build() - CANNOT be bypassed
PreHostValidationGuard.ValidateOrDie(builder.Configuration, builder.Environment, "Auth");

var envSmtpPass = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
if (!string.IsNullOrEmpty(envSmtpPass))
{
    builder.Configuration["Email:SmtpPassword"] = envSmtpPass;
}

// ── CENTRALIZED LOGGING ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ConfigureCentralizedLogging(builder.Configuration, "Auth", builder.Environment)
    .CreateLogger();

builder.Host.UseSerilog();

Log.Information("🔥 AUTH SERVICE: Starting with centralized logging");

try
{

// ── HTTPS ──────────────────────────────────────────────────────────────────────
builder.Services.AddHttpsRedirection(options => options.HttpsPort = 7001);

// ── Database ───────────────────────────────────────────────────────────────────
var authDbConnection = builder.Configuration.GetConnectionString("AuthDb")
    ?? throw new Exception("Database not configured");

builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseSqlServer(authDbConnection));

// ── RabbitMQ Event Publisher ──────────────────────────────────────────────────
builder.Services.AddSingleton<IEventPublisher>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ResilientRabbitMqPublisher>>();
    var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost";
    var rabbitPort = int.Parse(builder.Configuration["RabbitMq:Port"] ?? builder.Configuration["RabbitMQ:Port"] ?? "5672");
    var rabbitUserName = builder.Configuration["RabbitMq:Username"] ?? builder.Configuration["RabbitMQ:Username"] ?? "guest";
    var rabbitPassword = builder.Configuration["RabbitMq:Password"] ?? builder.Configuration["RabbitMQ:Password"] ?? "guest";
    var exchangeName = builder.Configuration["RabbitMq:Exchange"] ?? builder.Configuration["RabbitMQ:Exchange"] ?? "ecom-events";
    return new ResilientRabbitMqPublisher(rabbitHost, rabbitPort, rabbitUserName, rabbitPassword, exchangeName, logger);
});

// ── DI Registrations ───────────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdLogging();

// ── OpenTelemetry Distributed Tracing ─────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Ecom.Auth", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.request.method", request.Method);
                activity.SetTag("http.request.path", request.Path);
            };
        })
        .AddHttpClientInstrumentation()
        .AddSource(AuthActivitySource.SourceName)
        .AddConsoleExporter());

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AuditExportService>();
builder.Services.AddScoped<IEmailService, Ecom.Auth.Infrastructure.Services.EmailService>();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AuthMappingProfile>());
builder.Services.AddHostedService<TokenCleanupService>();

// ── Rate Limiting ──────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login: 5 requests per minute per IP
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Forgot password: 3 requests per minute per IP
    options.AddPolicy("forgot-password", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Refresh token: 10 requests per minute per IP
    options.AddPolicy("refresh", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Admin endpoints: 20 requests per minute per user
    options.AddPolicy("admin", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// ── JWT Authentication ─────────────────────────────────────────────────────────
// Auth Service is the ONLY service that generates tokens using symmetric key
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? throw new InvalidOperationException("JWT Key is not configured. Set JWT_KEY environment variable.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero  // No tolerance — token expires exactly on time
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    "{\"status\":401,\"message\":\"Unauthorized. Token is missing or invalid.\"}");
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(
                    "{\"status\":403,\"message\":\"Forbidden. You do not have permission to access this resource.\"}");
            }
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
                "http://localhost:61023",
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

// ── FluentValidation ───────────────────────────────────────────────────────────
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Ecom.Auth.Application.Validators.SignupRequestValidator>();

// ── Custom Validation Error Response Format ────────────────────────────────────
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value!.Errors.Count > 0)
            .Select(x => new
            {
                Field = x.Key,
                Errors = x.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            }).ToArray();

        var response = new
        {
            Status = 400,
            Message = "Validation failed",
            Errors = errors
        };

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});

// ── Health Checks ──────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        authDbConnection,
        name: "database",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? builder.Configuration["RabbitMQ:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? builder.Configuration["RabbitMQ:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? builder.Configuration["RabbitMQ:Host"] ?? "localhost"}:{builder.Configuration["RabbitMq:Port"] ?? builder.Configuration["RabbitMQ:Port"] ?? "5672"}",
        name: "rabbitmq",
        tags: new[] { "messaging", "rabbitmq" });

// ── Swagger ────────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Ecom Auth API", 
        Version = "v1",
        Description = "Authentication and User Management API for E-commerce Platform",
        Contact = new OpenApiContact
        {
            Name = "Auth Service Team"
        }
    });
    
    // Include XML comments for Swagger documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
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

// Database schema is managed separately via existing DB/migrations.

// ── Middleware Pipeline (ORDER MATTERS) ────────────────────────────────────────

// 1. Global exception handler — must be first to catch everything
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Correlation ID — before logging/auth so all logs have it
app.UseMiddleware<SharedCorrelationIdMiddleware>();

// 3. Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";
    
    // CSP: Relaxed in dev for Swagger, strict in production
    if (app.Environment.IsDevelopment())
    {
        context.Response.Headers["Content-Security-Policy"] = 
            "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;";
    }
    else
    {
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'";
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    
    await next();
});

// 4. Request logging
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled for local development with Gateway
app.UseCors("AllowAngular");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<UserLoggingMiddleware>();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "AUTH_SERVICE_FAILED");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
