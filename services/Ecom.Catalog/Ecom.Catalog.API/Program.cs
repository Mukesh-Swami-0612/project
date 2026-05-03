using Asp.Versioning;
using AspNetCoreRateLimit;
using Microsoft.OpenApi.Models;
using Ecom.Catalog.API.Middleware;
using Ecom.Catalog.Application.CQRS.Handlers;
using Ecom.Catalog.Application.Interfaces;
using Ecom.Catalog.Application.Mappings;
using Ecom.Catalog.Application.Services;
using Ecom.Catalog.Infrastructure.EventHandlers;
using Ecom.Catalog.Infrastructure.Persistence;
using Ecom.Catalog.Infrastructure.Repositories;
using Ecom.Catalog.Infrastructure.Messaging;
using Ecom.Catalog.Infrastructure.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Ecom.Shared.Infrastructure.Logging;
using Ecom.Shared.Infrastructure.Validation;
using Ecom.Catalog.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// 🔒 ABSOLUTE VALIDATION LOCKDOWN - PRE-HOST ENFORCEMENT
// Runs BEFORE builder.Build() - CANNOT be bypassed
// Zero dependency on logging or DI
PreHostValidationGuard.ValidateOrDie(builder.Configuration, builder.Environment, "Catalog");

var catalogDbConnection = builder.Configuration.GetConnectionString("CatalogDb")
    ?? throw new Exception("Database not configured");

// Configure Serilog with Centralized Logging
Log.Logger = new LoggerConfiguration()
    .ConfigureCentralizedLogging(builder.Configuration, "Catalog", builder.Environment)
    .CreateLogger();

try
{
    Log.Information("CATALOG_SERVICE_STARTING | Time: {Time}", DateTime.UtcNow);

// Use Serilog for logging
builder.Host.UseSerilog();

builder.Services.AddDbContext<CatalogDbContext>(opt =>
    opt.UseSqlServer(catalogDbConnection));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IBrandRepository, BrandRepository>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IReadModelRepository, ReadModelRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<OutboxService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<BrandService>();
builder.Services.AddScoped<MediaService>();
builder.Services.AddScoped<StorefrontService>();

// CQRS Handlers
builder.Services.AddScoped<CreateProductCommandHandler>();
builder.Services.AddScoped<UpdateProductCommandHandler>();
builder.Services.AddScoped<GetProductQueryHandler>();
builder.Services.AddScoped<ListProductsQueryHandler>();

// Event Handlers
builder.Services.AddScoped<ProductCreatedEventHandler>();
builder.Services.AddScoped<ProductUpdatedEventHandler>();

builder.Services.AddSingleton<CatalogEventPublisher>();
builder.Services.AddHostedService<OutboxProcessorService>();
builder.Services.AddHostedService<LogCleanupService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCorrelationIdLogging();
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<CatalogMappingProfile>());

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// ── JWT Authentication (Authority-based) ───────────────────────────────────────
// Catalog Service validates tokens issued by Auth Service (no shared secret)
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>("database")
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:Username"] ?? "guest"}:{builder.Configuration["RabbitMq:Password"] ?? "guest"}@{builder.Configuration["RabbitMq:Host"] ?? "localhost"}:{builder.Configuration["RabbitMq:Port"] ?? "5672"}",
        name: "catalog-rabbitmq",
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

// Apply migrations and log them
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var pendingMigrations = db.Database.GetPendingMigrations().ToList();
        
        if (pendingMigrations.Any())
        {
            logger.LogInformation("MIGRATIONS_PENDING | Count: {Count} | Migrations: {Migrations}", 
                pendingMigrations.Count, string.Join(", ", pendingMigrations));
            
            foreach (var migration in pendingMigrations)
            {
                logger.LogInformation("MIGRATION_APPLYING | Name: {Migration} | Time: {Time}", 
                    migration, DateTime.UtcNow);
            }
            
            db.Database.Migrate();
            
            // Get existing migration logs to avoid duplicates
            var existingMigrationLogs = db.MigrationLogs
                .Select(m => m.MigrationName)
                .ToHashSet();
            
            // Log only new migrations to database
            foreach (var migration in pendingMigrations)
            {
                if (!existingMigrationLogs.Contains(migration))
                {
                    var serviceIdentity = builder.Configuration["ServiceIdentity"] ?? "Catalog-Service";
                    var appliedBy = $"{serviceIdentity}@{Environment.MachineName}";
                    
                    var migrationLog = new MigrationLog
                    {
                        MigrationName = migration,
                        AppliedBy = appliedBy,
                        AppliedAt = DateTime.UtcNow,
                        Details = $"Applied automatically on service startup by {serviceIdentity}"
                    };
                    
                    db.MigrationLogs.Add(migrationLog);
                    
                    logger.LogInformation("MIGRATION_APPLIED | Name: {Migration} | By: {User} | Time: {Time}", 
                        migration, migrationLog.AppliedBy, migrationLog.AppliedAt);
                }
            }
            
            await db.SaveChangesAsync();
        }
        else
        {
            logger.LogInformation("MIGRATIONS_UP_TO_DATE | Time: {Time}", DateTime.UtcNow);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "MIGRATION_FAILED | Error: {Error} | Time: {Time}", 
            ex.Message, DateTime.UtcNow);
        throw;
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseIpRateLimiting();
app.UseCors("AllowAngular"); // 🔥 CORS: Enable cross-origin requests from Angular
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseMiddleware<UserLogContextMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("CATALOG_SERVICE_STARTED | Time: {Time}", DateTime.UtcNow);
app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "CATALOG_SERVICE_FAILED | Error: {Error} | Time: {Time}", 
        ex.Message, DateTime.UtcNow);
    throw;
}
finally
{
    Log.Information("CATALOG_SERVICE_SHUTDOWN | Time: {Time}", DateTime.UtcNow);
    Log.CloseAndFlush();
}

