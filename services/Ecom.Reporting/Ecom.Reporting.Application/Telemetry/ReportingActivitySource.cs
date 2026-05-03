using System.Diagnostics;

namespace Ecom.Reporting.Application.Telemetry;

/// <summary>
/// ActivitySource for distributed tracing in Reporting service
/// </summary>
public static class ReportingActivitySource
{
    public const string SourceName = "Ecom.Reporting";
    
    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
