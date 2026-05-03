using System.Diagnostics;

namespace Ecom.Catalog.Application.Telemetry;

/// <summary>
/// ActivitySource for distributed tracing in Catalog service
/// </summary>
public static class CatalogActivitySource
{
    public const string SourceName = "Ecom.Catalog";
    
    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
