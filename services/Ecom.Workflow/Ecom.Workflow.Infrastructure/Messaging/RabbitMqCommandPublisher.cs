using System.Text;
using System.Text.Json;
using Ecom.Workflow.Application.Interfaces;
using Ecom.Workflow.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Ecom.Workflow.Infrastructure.Messaging;

/// <summary>
/// Publishes commands to other services via RabbitMQ
/// Commands are sent to catalog-commands exchange
/// </summary>
public class RabbitMqCommandPublisher : ICommandPublisher, IDisposable
{
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMqCommandPublisher> _logger;
    private readonly object _connectionLock = new();
    private readonly ResiliencePipeline<bool> _resiliencePolicy;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed;

    public RabbitMqCommandPublisher(
        IConfiguration config,
        ILogger<RabbitMqCommandPublisher> logger)
    {
        _config = config;
        _logger = logger;
        _resiliencePolicy = ResiliencePolicies.CreateCombinedPolicy<bool>(logger);
        InitializeConnection();
    }

    private void InitializeConnection()
    {
        try
        {
            CloseRabbitMqResources();
            _disposed = false;

            var factory = new ConnectionFactory
            {
                HostName = _config["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_config["RabbitMq:Port"] ?? "5672"),
                UserName = _config["RabbitMq:Username"] ?? "guest",
                Password = _config["RabbitMq:Password"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare commands exchange
            _channel.ExchangeDeclare(
                exchange: "catalog-commands",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ Command Publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Command Publisher");
            throw;
        }
    }

    public async Task PublishAsync<T>(string routingKey, T command) where T : class
    {
        try
        {
            // 🔥 RESILIENCE: Apply retry + circuit breaker policy
            await _resiliencePolicy.ExecuteAsync(async cancellationToken =>
            {
                lock (_connectionLock)
                {
                    if (_channel == null || !_channel.IsOpen)
                    {
                        _logger.LogWarning("RabbitMQ channel not available, reinitializing...");
                        InitializeConnection();
                    }

                    var message = JsonSerializer.Serialize(command);
                    var body = Encoding.UTF8.GetBytes(message);

                    var properties = _channel!.CreateBasicProperties();
                    properties.Persistent = true;
                    properties.ContentType = "application/json";
                    properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                    _channel.BasicPublish(
                        exchange: "catalog-commands",
                        routingKey: routingKey,
                        basicProperties: properties,
                        body: body);
                }

                _logger.LogInformation(
                    "COMMAND_PUBLISHED | RoutingKey: {RoutingKey} | Command: {CommandType}",
                    routingKey,
                    typeof(T).Name);

                return true;
            }, CancellationToken.None);
        }
        catch (Polly.CircuitBreaker.BrokenCircuitException ex)
        {
            _logger.LogError(
                "COMMAND_PUBLISH_CIRCUIT_OPEN | RoutingKey: {RoutingKey} | Command: {CommandType} | Circuit breaker is open, operation rejected",
                routingKey,
                typeof(T).Name);
            throw new InvalidOperationException("RabbitMQ circuit breaker is open. Service temporarily unavailable.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "COMMAND_PUBLISH_FAILED | RoutingKey: {RoutingKey} | Command: {CommandType} | Error: {Error}",
                routingKey,
                typeof(T).Name,
                ex.Message);
            throw;
        }
    }

    public void Dispose()
    {
        CloseRabbitMqResources();
        GC.SuppressFinalize(this);
    }

    private void CloseRabbitMqResources()
    {
        lock (_connectionLock)
        {
            if (_disposed)
            {
                return;
            }

            SafeClose(_channel, "RabbitMQ command channel");
            SafeClose(_connection, "RabbitMQ command connection");

            SafeDispose(_channel, "RabbitMQ command channel");
            SafeDispose(_connection, "RabbitMQ command connection");
            _channel = null;
            _connection = null;
            _disposed = true;
        }
    }

    private void SafeClose(IModel? channel, string resourceName)
    {
        if (channel == null)
        {
            return;
        }

        try
        {
            if (channel.IsOpen)
            {
                channel.Close();
            }
        }
        catch (AlreadyClosedException)
        {
            _logger.LogDebug("{ResourceName} was already closed", resourceName);
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            _logger.LogDebug("{ResourceName} channel was already closed", resourceName);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("{ResourceName} was already disposed", resourceName);
        }
    }

    private void SafeClose(IConnection? connection, string resourceName)
    {
        if (connection == null)
        {
            return;
        }

        try
        {
            if (connection.IsOpen)
            {
                connection.Close();
            }
        }
        catch (AlreadyClosedException)
        {
            _logger.LogDebug("{ResourceName} was already closed", resourceName);
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            _logger.LogDebug("{ResourceName} channel was already closed", resourceName);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("{ResourceName} was already disposed", resourceName);
        }
    }

    private void SafeDispose(IDisposable? disposable, string resourceName)
    {
        if (disposable == null)
        {
            return;
        }

        try
        {
            disposable.Dispose();
        }
        catch (AlreadyClosedException)
        {
            _logger.LogDebug("{ResourceName} was already closed before dispose", resourceName);
        }
        catch (System.Threading.Channels.ChannelClosedException)
        {
            _logger.LogDebug("{ResourceName} channel was already closed before dispose", resourceName);
        }
        catch (ObjectDisposedException)
        {
            _logger.LogDebug("{ResourceName} was already disposed", resourceName);
        }
    }
}
