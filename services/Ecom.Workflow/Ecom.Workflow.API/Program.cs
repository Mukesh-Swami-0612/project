using System.Text;
using Microsoft.OpenApi.Models;
using Ecom.Workflow.API.Middleware;
using Ecom.Workflow.Application.Configuration;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Telemetry;
using Ecom.Workflow.Application.Mappings;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Infrastructure.BackgroundServices;
using Ecom.Workflow.Infrastructure.Messaging;
using Ecom.Workflow.Infrastructure.Messaging.Consumers;
using Ecom.Workflow.Infrastructure.Persistence;
using Ecom.Workflow.Infrastructure.Persistence.Repositories;
using Ecom.Workflow.Infrastructure.Repositories;
using Ecom.Workflow.Infrastructure.Services;
using Ecom.Shared.Infrastructure.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Ecom.Shared.Infrastructure.Logging;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// 🔒 ABSOLUTE VALIDATION LOCKDOWN - PRE-HOST ENFORCEMENT
// Runs BEFORE builder.Build() - CANNOT be bypassed
PreHostValidationGuard.ValidateOrDie(builder.Configuration, builder.Environment, "Workflow");

Log.Logger = new LoggerConfiguration()
    .ConfigureCentralizedLogging(builder.Configuration, "Workflow", builder.Environment)
    .CreateLogger();

builder.Host.UseSerilog();

var workflowDbConnection = builder.Configuration.GetConnectionString("WorkflowDb")
    ?? throw new Exception("Database not configured");

builder.Services.AddDbContext<WorkflowDbContext>(opt =>
    opt.UseSqlServer(workflowDbConnection));

// Repositories
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IWorkflowInstanceRepository, WorkflowInstanceRepository>();
builder.Services.AddScoped<IWorkflowAuditRepository, WorkflowAuditRepository>();
builder.Services.AddScoped<ISagaCompensationRepository, SagaCompensationRepository>();

// Services
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IPublishService, PublishService>();
builder.Services.AddScoped<ProductSagaOrchestrator>();
builder.Services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();
builder.Services.AddScoped<IWorkflowCompensationService, WorkflowCompensationService>();
builder.Services.AddScoped<IWorkflowExportService, WorkflowExportService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.Configure<IdempotencyOptions>(builder.Configuration.GetSection(IdempotencyOptions.SectionName));

// 🔥 AUDIT SERVICE: Business event logging with username + timestamp
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdLogging();
builder.Services.AddScoped<IWorkflowAuditService, WorkflowAuditService>();

// ── OpenTelemetry Distributed Tracing ─────────────────────────────────────────
// 🔥 PRODUCTION: Jaeger exporter for distributed tracing visualization
var jaegerEndpoint = builder.Configuration["Jaeger:Endpoint"] ?? "http://localhost:14268/api/traces";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("Ecom.Workflow", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        })
        .AddHttpClientInstrumentation()
        .AddSource(WorkflowActivitySource.SourceName)
        .AddJaegerExporter(options =>
        {
            options.Endpoint = new Uri(jaegerEndpoint);
        })
        .AddConsoleExporter()); // Keep console for local development

// Messaging
builder.Services.AddScoped<WorkflowEventPublisher>();
builder.Services.AddSingleton<ICommandPublisher, RabbitMqCommandPublisher>();
builder.Services.AddSingleton<ProductCreatedConsumer>();
builder.Services.AddSingleton<ProductValidatedConsumer>();
builder.Services.AddSingleton<ProductApprovedConsumer>();
builder.Services.AddSingleton<ProductPublishedConsumer>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<RabbitMqConsumerService>();
builder.Services.AddHostedService<WorkflowRetryService>();
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<WorkflowMappingProfile>();
});

// ── JWT Authentication (Authority-based) ───────────────────────────────────────
// Workflow Service validates tokens issued by Auth Service (no shared secret)
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
builder.Services.AddEndpointsApiExplorer();

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: workflowDbConnection,
        name: "workflow-database",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? "localhost"}:{builder.Configuration["RabbitMq:Port"] ?? "5672"}",
        name: "workflow-rabbitmq",
        tags: new[] { "messaging", "rabbitmq" });

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
app.UseCors("AllowAngular"); // 🔥 CORS: Enable cross-origin requests from Angular
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseMiddleware<UserLogContextMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
