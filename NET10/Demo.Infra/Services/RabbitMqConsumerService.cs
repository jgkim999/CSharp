using System.Diagnostics;
using Demo.Infra.Configs;
using System.Text;
using Demo.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OpenTelemetry.Context.Propagation;

namespace Demo.Infra.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly string _hostName;
    private readonly string _queueName;
    private readonly RabbitMqConfig _config;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new <see cref="RabbitMqConsumerService"/> instance, reading RabbitMQ settings from <paramref name="config"/>,
    /// establishing a connection and channel to the broker, and declaring the configured queue.
    /// </summary>
    /// <param name="config">Options wrapper containing <see cref="RabbitMqConfig"/>; HostName and QueueName from this value are used to connect and declare the queue.</param>
    /// <param name="logger">Logger used by the service.</param>
    /// <param name="telemetryService">Telemetry service used to create activities for incoming messages.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> or <paramref name="telemetryService"/> is null.</exception>
    public RabbitMqConsumerService(IOptions<RabbitMqConfig> config, ILogger<RabbitMqConsumerService> logger, ITelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(config);
        _telemetryService = telemetryService;
        _logger = logger;
        _config = config.Value;
        _hostName = _config.HostName;
        _queueName = _config.QueueName;

        var factory = new ConnectionFactory
        {
            //UserName = ConnectionFactory.DefaultUser,
            //Password = ConnectionFactory.DefaultPass,
            //VirtualHost = ConnectionFactory.DefaultVHost,
            HostName = _hostName,
            //Port = AmqpTcpEndpoint.UseDefaultPort,
            //MaxInboundMessageBodySize = 512 * 1024 * 1024
            AutomaticRecoveryEnabled = true
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(queue: _queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);
    }
    
    /// <summary>
    /// Runs the background consumer loop: repeatedly calls <see cref="ConsumeMessagesAsync(CancellationToken)"/> to process messages until the provided cancellation token is signaled.
    /// </summary>
    /// <param name="stoppingToken">Token that requests graceful shutdown; when signaled the method exits the loop and completes.</param>
    /// <returns>A task that completes when the service stops (when <paramref name="stoppingToken"/> is canceled or an unrecoverable error occurs).</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeMessagesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(RabbitMqConsumerService));
            }
        }
    }

    /// <summary>
    /// Registers an asynchronous RabbitMQ consumer for the configured queue and begins processing incoming messages.
    /// </summary>
    /// <remarks>
    /// For each received message this method:
    /// - extracts OpenTelemetry trace context from message headers and starts a consumer Activity with that context,
    /// - logs the message body (UTF-8),
    /// - uses automatic acknowledgement (autoAck = true).
    /// Message processing runs on the consumer's event handler until the provided <paramref name="stoppingToken"/> is cancelled.
    /// </remarks>
    /// <param name="stoppingToken">Token used to stop registering/consuming messages; when cancelled the method will stop awaiting consumption setup or exit if cancelled during the setup call.</param>
    /// <returns>A <see cref="ValueTask"/> that completes after the consumer is started (the method registers the handler and awaits the underlying BasicConsumeAsync call).</returns>
    private async ValueTask ConsumeMessagesAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.ReceivedAsync += async (model, ea) =>
        {
            // 메시지 헤더에서 trace context 추출
            var propagator = Propagators.DefaultTextMapPropagator;
            var parentContext = propagator.Extract(default, ea.BasicProperties?.Headers, 
                (headers, key) =>
                {
                    if (headers != null && headers.TryGetValue(key, out var value) && value is byte[] bytes)
                    {
                        return [Encoding.UTF8.GetString(bytes)];
                    }
                    return [];
                });
            
            // 추출된 parent context를 사용하여 Consumer Activity 생성
            using var activity = _telemetryService.StartActivity(
                nameof(ConsumeMessagesAsync), 
                ActivityKind.Consumer, 
                parentContext.ActivityContext,
                new Dictionary<string, object?>
                {
                    ["messaging.system"] = "rabbitmq",
                    ["messaging.destination"] = _queueName,
                    ["messaging.operation"] = "receive"
                });
            
            ReadOnlySpan<byte> bodySpan = ea.Body.Span;
            var message = Encoding.UTF8.GetString(bodySpan);
            _logger.LogInformation("Received message: {Message}", message);
            await Task.CompletedTask;
        };
        await _channel.BasicConsumeAsync(queue: _queueName, autoAck: true, consumer: consumer, stoppingToken);
    }
}