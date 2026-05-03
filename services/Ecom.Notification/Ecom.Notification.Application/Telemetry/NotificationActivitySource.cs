using System.Diagnostics;

namespace Ecom.Notification.Application.Telemetry;

/// <summary>
/// ActivitySource for distributed tracing in Notification service
/// </summary>
public static class NotificationActivitySource
{
    public const string SourceName = "Ecom.Notification";
    
    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
