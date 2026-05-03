using Ecom.Workflow.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ecom.Workflow.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that processes workflows due for retry
/// Implements exponential backoff strategy
/// Runs every 5 seconds to check for due retries
/// </summary>
public class WorkflowRetryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkflowRetryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(5);

    public WorkflowRetryService(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkflowRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Workflow Retry Service starting...");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow Retry Service is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueRetriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workflow retries");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessDueRetriesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceRepository>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkflowOrchestrator>();

        var currentTime = DateTime.UtcNow;
        var dueWorkflows = await repo.GetDueRetriesAsync(currentTime);

        foreach (var workflow in dueWorkflows)
        {
            try
            {
                _logger.LogInformation(
                    "WORKFLOW_RETRY_PROCESSING | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RetryCount: {RetryCount} | ScheduledAt: {NextRetryAt}",
                    workflow.Id,
                    workflow.ProductId,
                    workflow.RetryCount,
                    workflow.NextRetryAt);

                // Clear NextRetryAt to prevent duplicate processing
                workflow.NextRetryAt = null;
                await repo.UpdateAsync(workflow);

                // Retry orchestration
                await orchestrator.ProcessAsync(workflow.Id);

                _logger.LogInformation(
                    "WORKFLOW_RETRY_COMPLETED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RetryCount: {RetryCount}",
                    workflow.Id,
                    workflow.ProductId,
                    workflow.RetryCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "WORKFLOW_RETRY_FAILED | WorkflowId: {WorkflowId} | ProductId: {ProductId} | RetryCount: {RetryCount} | Error: {Error}",
                    workflow.Id,
                    workflow.ProductId,
                    workflow.RetryCount,
                    ex.Message);

                // MarkAsFailed will be called by orchestrator's catch block
            }
        }

        if (dueWorkflows.Any())
        {
            _logger.LogInformation(
                "WORKFLOW_RETRY_BATCH_COMPLETED | ProcessedCount: {Count}",
                dueWorkflows.Count());
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Workflow Retry Service stopping...");
        return base.StopAsync(cancellationToken);
    }
}
