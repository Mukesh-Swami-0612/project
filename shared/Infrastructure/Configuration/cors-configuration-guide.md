# 🔥 CORS Configuration Guide

## Overview
This document outlines the consistent CORS (Cross-Origin Resource Sharing) configuration implemented across all microservices to ensure seamless communication with the Angular frontend.

## Configuration Details

### Allowed Origins
- `http://localhost:4200` - Angular development server (HTTP)
- `https://localhost:4200` - Angular development server (HTTPS)
- `http://localhost:3000` - Alternative development port
- `https://localhost:3000` - Alternative development port (HTTPS)

### CORS Policy Settings
```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials() // Required for HTTP-only cookies
      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
```

### Security Features
- ✅ **AllowCredentials**: Enables HTTP-only cookie transmission
- ✅ **AllowAnyHeader**: Supports all request headers (Authorization, Content-Type, etc.)
- ✅ **AllowAnyMethod**: Supports all HTTP methods (GET, POST, PUT, DELETE, etc.)
- ✅ **Preflight Caching**: 10-minute cache for OPTIONS requests

## Service Implementation Status

| Service | CORS Configured | Policy Name | Middleware Order |
|---------|----------------|-------------|------------------|
| Gateway | ✅ | AllowAll | ✅ Correct |
| Auth | ✅ | AllowAngular | ✅ Correct |
| Catalog | ✅ | AllowAngular | ✅ Correct |
| Workflow | ✅ | AllowAngular | ✅ Correct |
| Reporting | ✅ | AllowAngular | ✅ Correct |
| Notification | ✅ | AllowAngular | ✅ Correct |

## Middleware Order
The CORS middleware is positioned correctly in all services:

```csharp
app.UseHttpsRedirection();
app.UseCors("AllowAngular");        // 🔥 CORS before authentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

## Testing CORS Configuration

### Manual Testing
```bash
# Test preflight request
curl -X OPTIONS \
  -H "Origin: http://localhost:4200" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type,Authorization" \
  https://localhost:7001/api/v1/auth/login

# Expected headers in response:
# Access-Control-Allow-Origin: http://localhost:4200
# Access-Control-Allow-Methods: POST
# Access-Control-Allow-Headers: Content-Type,Authorization
# Access-Control-Allow-Credentials: true
```

### Automated Testing
Use the `CorsTestHelper` utility to test all services:

```csharp
var corsTestHelper = new CorsTestHelper(httpClient, logger);
var results = await corsTestHelper.TestAllServicesAsync();
var report = corsTestHelper.GenerateReport(results);
Console.WriteLine(report);
```

## Common Issues and Solutions

### Issue: CORS errors in browser console
**Solution**: Verify the service has CORS configured and the middleware order is correct.

### Issue: Credentials not being sent
**Solution**: Ensure `AllowCredentials()` is set and frontend uses `withCredentials: true`.

### Issue: Preflight requests failing
**Solution**: Check that OPTIONS method is allowed and proper headers are configured.

## Production Considerations

### Environment-Specific Origins
For production deployment, update allowed origins:

```csharp
var allowedOrigins = builder.Environment.IsDevelopment() 
    ? new[] { "http://localhost:4200", "https://localhost:4200" }
    : new[] { "https://yourdomain.com", "https://www.yourdomain.com" };

policy.WithOrigins(allowedOrigins)
```

### Security Best Practices
- ✅ Use specific origins instead of `AllowAnyOrigin()`
- ✅ Enable `AllowCredentials()` only when needed
- ✅ Set appropriate preflight cache duration
- ✅ Monitor CORS-related logs for security issues

## Troubleshooting

### Browser Developer Tools
1. Open Network tab
2. Look for OPTIONS requests (preflight)
3. Check response headers for CORS configuration
4. Verify no CORS errors in console

### Server Logs
Monitor logs for CORS-related entries:
```
[INFO] CORS preflight request from http://localhost:4200
[INFO] CORS actual request allowed for http://localhost:4200
```

## Configuration Validation

All services now consistently:
- ✅ Accept requests from Angular frontend origins
- ✅ Allow credentials for HTTP-only cookie authentication
- ✅ Support all necessary headers and methods
- ✅ Have correct middleware ordering
- ✅ Cache preflight requests for performance