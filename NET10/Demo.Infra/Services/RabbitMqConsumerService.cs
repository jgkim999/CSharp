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