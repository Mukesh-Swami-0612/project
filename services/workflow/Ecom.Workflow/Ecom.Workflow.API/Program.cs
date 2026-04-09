using System.Text;
using Microsoft.OpenApi.Models;
using Ecom.Workflow.API.Middleware;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Application.Mappings;
using Ecom.Workflow.Application.Services;
using Ecom.Workflow.Infrastructure.Messaging;
using Ecom.Workflow.Infrastructure.Persistence;
using Ecom.Workflow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WorkflowDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("WorkflowDb")));

builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<IPublishService, PublishService>();
builder.Services.AddScoped<WorkflowEventPublisher>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddAutoMapper(config =>
{
    config.AddProfile<WorkflowMappingProfile>();
});

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
builder.Services.AddEndpointsApiExplorer();

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("WorkflowDb")!,
        name: "workflow-database",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddRabbitMQ(
        sp =>
        {
            var factory = new RabbitMQ.Client.ConnectionFactory
            {
                HostName = builder.Configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(builder.Configuration["RabbitMq:Port"] ?? "5672"),
                UserName = builder.Configuration["RabbitMq:Username"] ?? "guest",
                Password = builder.Configuration["RabbitMq:Password"] ?? "guest"
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        },
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
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

