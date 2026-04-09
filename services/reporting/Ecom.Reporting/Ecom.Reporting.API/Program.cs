using System.Text;
using Microsoft.OpenApi.Models;
using Ecom.Reporting.API.Middleware;
using Ecom.Reporting.Application.Interfaces;
using Ecom.Reporting.Application.Mappings;
using Ecom.Reporting.Application.Services;
using Ecom.Reporting.Infrastructure.Persistence;
using Ecom.Reporting.Infrastructure.Repositories;
using Ecom.Reporting.Infrastructure.Messaging.Consumers;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ReportingDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ReportingDb")));

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddAutoMapper(typeof(ReportingMappingProfile));

// RabbitMQ Consumers
builder.Services.AddHostedService<InventoryUpdatedConsumer>();
builder.Services.AddHostedService<PricingUpdatedConsumer>();
builder.Services.AddHostedService<ProductApprovedConsumer>();
builder.Services.AddHostedService<ProductPublishedConsumer>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("ReportingDb")!,
        name: "database",
        tags: new[] { "db", "sql" });

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true,
            ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

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
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");

app.Run();
