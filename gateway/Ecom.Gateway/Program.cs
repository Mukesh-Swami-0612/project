using System.Text;
using Ecom.Gateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"ocelot.{builder.Environment.EnvironmentName}.json", optional: true);

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
     
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
        
        // 🔥 CRITICAL: Read JWT token from HTTP-only cookie
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // First check Authorization header (for backward compatibility)
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
                // If no Authorization header, check HTTP-only cookie
                else if (context.Request.Cookies.ContainsKey("accessToken"))
                {
                    context.Token = context.Request.Cookies["accessToken"];
                }
                
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200", 
                "https://localhost:4200",
                "http://localhost:61023",
                "http://localhost:62295"  // Add the actual port Angular is using
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOcelot();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseMiddleware<GatewayLoggingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
await app.UseOcelot();
app.Run();
