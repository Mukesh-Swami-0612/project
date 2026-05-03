using Ecom.Auth.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Ecom.Auth.Infrastructure.Messaging;

public class AuthEventPublisher
{
    private readonly ILogger<AuthEventPublisher> _logger;

    public AuthEventPublisher(ILogger<AuthEventPublisher> logger) => _logger = logger;

    public Task PublishUserCreatedAsync(UserCreatedEvent @event)
    {
        // Publish to RabbitMQ exchange: ecom.domain.events, routing key: user.created
        _logger.LogInformation("Publishing UserCreatedEvent for UserId: {UserId}", @event.UserId);
        return Task.CompletedTask;
    }
}
