using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Ecom.Shared.Infrastructure.Validation;

/// <summary>
/// Minimal IHostEnvironment implementation for pre-host validation
/// Used when validation runs before builder is created
/// </summary>
public class BootstrapHostEnvironment : IHostEnvironment
{
    public BootstrapHostEnvironment(string environmentName)
    {
        EnvironmentName = environmentName;
        ApplicationName = "Ecom";
        ContentRootPath = Directory.GetCurrentDirectory();
        ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
    }

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; }
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}
