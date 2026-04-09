using Ecom.Workflow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Workflow.Infrastructure.Messaging;

// Background service that polls OutboxEvents and publishes to RabbitMQ
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider services, ILogger<OutboxProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var publisher = scope.ServiceProvider.GetRequiredService<WorkflowEventPublisher>();

            var events = await outbox.GetUnprocessedAsync();
            foreach (var ev in events)
            {
                await publisher.PublishAsync(ev.EventType!, ev.Payload!);
                await outbox.MarkProcessedAsync(ev.Id);
                _logger.LogInformation("Processed outbox event {Id}: {EventType}", ev.Id, ev.EventType);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
