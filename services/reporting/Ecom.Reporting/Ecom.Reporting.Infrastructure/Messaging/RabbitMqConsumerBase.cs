using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Ecom.Reporting.Infrastructure.Messaging;

public abstract class RabbitMqConsumerBase : BackgroundService
{
    protected IConnection? _connection;
    protected IModel? _channel;
    protected readonly IConfiguration _configuration;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;
    
    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    protected RabbitMqConsumerBase(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            InitializeRabbitMQ();
            
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogWarning("RabbitMQ channel not available for {Queue}", QueueName);
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    
                    _logger.LogInformation("Received message on {Queue}: {Message}", QueueName, message);
                    
                    await ProcessMessageAsync(message);
                    
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message on {Queue}", QueueName);
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            
            _logger.LogInformation("Consumer started for {Queue}", QueueName);
            
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in consumer {Queue}", QueueName);
        }
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.Parse(_configuration["RabbitMq:Port"] ?? "5672"),
                UserName = _configuration["RabbitMq:Username"] ?? "guest",
                Password = _configuration["RabbitMq:Password"] ?? "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: "ecom.domain.events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: QueueName,
                exchange: "ecom.domain.events",
                routingKey: RoutingKey);

            _logger.LogInformation("RabbitMQ initialized for {Queue} with routing key {RoutingKey}", 
                QueueName, RoutingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ for {Queue}", QueueName);
        }
    }

    protected abstract Task ProcessMessageAsync(string message);

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
