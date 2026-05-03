using Microsoft.Extensions.Hosting;

namespace Ecom.Notification.Application.Services;

/// <summary>
/// Service for loading and processing HTML email templates
/// Supports placeholder replacement for dynamic content
/// </summary>
public class EmailTemplateService
{
    private readonly IHostEnvironment _env;

    public EmailTemplateService(IHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Load an HTML template from the Templates folder
    /// </summary>
    /// <param name="templateName">Template filename (e.g., "welcome.html")</param>
    /// <returns>Raw HTML template content</returns>
    public string LoadTemplate(string templateName)
    {
        var path = Path.Combine(_env.ContentRootPath, "Templates", templateName);
        
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Email template not found: {templateName}", path);
        }
        
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Replace placeholders in template with actual data
    /// Placeholders format: {{PlaceholderName}}
    /// </summary>
    /// <param name="template">HTML template with placeholders</param>
    /// <param name="data">Dictionary of placeholder names and values</param>
    /// <returns>HTML with replaced values</returns>
    public string ReplacePlaceholders(string template, Dictionary<string, string> data)
    {
        foreach (var item in data)
        {
            template = template.Replace($"{{{{{item.Key}}}}}", item.Value);
        }
        
        return template;
    }
}
