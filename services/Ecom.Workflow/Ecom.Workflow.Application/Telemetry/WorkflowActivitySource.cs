using System.Diagnostics;

namespace Ecom.Workflow.Application.Telemetry;

/// <summary>
/// ActivitySource for distributed tracing in Workflow service
/// </summary>
public static class WorkflowActivitySource
{
    public const string SourceName = "Ecom.Workflow";
    
    public static readonly ActivitySource Instance = new(SourceName, "1.0.0");
}
