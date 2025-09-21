using Demo.Infra.Configs;
using Demo.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Demo.Domain.Enums;

namespace Demo.Infra.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    private readonly RabbitMqHandler _handler;
    
    private readonly string _multiQueue;

    public RabbitMqConsumerService(
        IOptions<RabbitMqConfig> config,
        RabbitMqConnection connection,
        RabbitMqHandler handler,
        ILogger<RabbitMqConsumerService> logger,
        ITelemetryService telemetryService)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(config);
        _logger = logger;
        _connection = connection;
        _telemetryService = telemetryService;
        _handler = handler;

        // Multi: 각 Consumer마다 고유한 queue (fanout으로 모든 Consumer에게 전송)
        _multiQueue = config.Value.QueueName + ".multi." + Ulid.NewUlid();

        _connection.Channel.QueueDeclareAsync(queue: _multiQueue,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);
        
        // Multi: fanout exchange - routing key 무시되므로 빈 문자열 사용
        _connection.Channel.QueueBindAsync(
            queue: _multiQueue,
            exchange: _connection.ProducerExchangeMulti,
            routingKey: "",
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
        // Multi queue consumer
        var multiConsumer = new AsyncEventingBasicConsumer(_connection.Channel);
        multiConsumer.ReceivedAsync += async (model, ea) =>
        {
            await ProcessMessageAsync(MqSenderType.Multi, ea, stoppingToken);
        };

        // Any queue consumer
        var anyConsumer = new AsyncEventingBasicConsumer(_connection.Channel);
        anyConsumer.ReceivedAsync += async (model, ea) =>
        {
            await ProcessMessageAsync(MqSenderType.Any, ea, stoppingToken);
        };

        // queue에서 메시지 수신 시작 (autoAck: false로 수동 Ack 설정)
        await _connection.Channel.BasicConsumeAsync(queue: _multiQueue, autoAck: false, consumer: multiConsumer, stoppingToken);
        await _connection.Channel.BasicConsumeAsync(queue: _connection.AnyQueue, autoAck: false, consumer: anyConsumer, stoppingToken);
        
        // 무한 대기하며 메시지 처리
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(MqSenderType queueType, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            await _handler.HandleAsync(queueType, ea, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {QueueType} queue", queueType);
        }
    }
}