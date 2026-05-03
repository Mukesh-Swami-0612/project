using System.Diagnostics;

namespace Ecom.Auth.Application.Telemetry;

/// <summary>
/// ActivitySource for distributed tracing in Auth service
/// </summary>
public static class AuthActivitySource
{
    public const string SourceName = "Ecom.Auth";
    
    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
