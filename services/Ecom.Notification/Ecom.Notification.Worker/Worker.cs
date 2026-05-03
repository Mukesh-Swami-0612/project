using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Ecom.Notification.Application.DTOs;
using Ecom.Notification.Application.Interfaces;
using Ecom.Notification.Infrastructure.Persistence;
using System.Linq;
using Serilog.Context;

namespace Ecom.Notification.Worker;

/// <summary>
/// Background worker that processes pending notifications
/// Handles both scheduled and event-driven notifications
/// </summary>
public class Worker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<Worker> _logger;

    public Worker(IServiceProvider services, ILogger<Worker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NOTIFICATION_WORKER_STARTED");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var now = DateTime.UtcNow;

                // Fetch pending notifications with exponential backoff support:
                // 1. Status = Pending
                // 2. Either no schedule OR scheduled time has passed
                // 3. Either no retry scheduled OR retry time has passed (exponential backoff)
                // 4. Retry count < 3
                var pendingNotifications = db.Notifications
                    .Where(x => x.Status == "Pending" &&
                                (!x.ScheduledAt.HasValue || x.ScheduledAt.Value <= now) &&
                                (!x.NextRetryAt.HasValue || x.NextRetryAt.Value <= now) &&
                                x.RetryCount < 3)
                    .ToList();

                if (pendingNotifications.Any())
                {
                    _logger.LogInformation("NOTIFICATION_WORKER_PROCESSING_PENDING | Count: {Count}", pendingNotifications.Count);

                    foreach (var notification in pendingNotifications)
                    {
                        using (LogContext.PushProperty("CorrelationId", notification.CorrelationId))
                        using (LogContext.PushProperty("NotificationId", notification.Id))
                        {
                            _logger.LogInformation(
                                "NOTIFICATION_WORKER_PROCESSING_NOTIFICATION | Type: {Type} | Email: {Email} | Retry: {RetryCount} | NextRetryAt: {NextRetryAt}",
                                notification.Type,
                                notification.ToEmail,
                                notification.RetryCount,
                                notification.NextRetryAt);

                            await service.ProcessScheduledAsync(notification);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NOTIFICATION_WORKER_ERROR");
            }

            // Polling interval: 5 seconds (faster for event-driven notifications)
            await Task.Delay(5000, stoppingToken);
        }
    }
}
