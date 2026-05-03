using System.Text.Json.Nodes;
using Ecom.Notification.Application.DTOs;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ecom.Notification.API.Swagger;

public class ScheduledAtIstSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(ScheduleNotificationRequest))
            return;

        if (schema.Properties != null && schema.Properties.TryGetValue("scheduledAtIST", out var scheduledAtIstProperty))
        {
            if (scheduledAtIstProperty is OpenApiSchema writableSchema)
            {
                var currentIst = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(5.5));
                writableSchema.Example = JsonValue.Create(currentIst.ToString("yyyy-MM-ddTHH:mm:sszzz"));
                writableSchema.Description = "IST datetime (UTC+05:30)";
            }
        }
    }
}
