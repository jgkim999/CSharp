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

        // Multi: fanout exchange - routing key 무시되므로 빈 문자열 사용
        _connection.Channel.ExchangeDeclareAsync(
            exchange: _connection.MultiExchange,
            type: ExchangeType.Fanout)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        _logger.LogInformation("MultiExchange {ExchangeName}", _connection.MultiExchange);
        
        _connection.Channel.QueueDeclareAsync(queue: _connection.MultiQueue,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        _logger.LogInformation("MultiQueue {QueueName}", _connection.MultiQueue);
        
        // Multi: fanout exchange - routing key 무시되므로 빈 문자열 사용
        _connection.Channel.QueueBindAsync(
            queue: _connection.MultiQueue,
            exchange: _connection.MultiExchange,
            routingKey: "",
            arguments: null)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        _logger.LogInformation(
            "MultiQueue {QueueName} bound to {ExchangeName}",
            _connection.MultiQueue, _connection.MultiExchange);
        
        // Any
        _connection.Channel.QueueDeclareAsync(
            queue: _connection.AnyQueue,
            durable: true, // 서버 재시작 시에도 유지
            exclusive: false,
            autoDelete: false, // 공유 queue이므로 자동 삭제 비활성화
            arguments: null)
            .ConfigureAwait(false).GetAwaiter().GetResult();
        _logger.LogInformation("Any queue declared: {QueueName}", _connection.AnyQueue);
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
        await _connection.Channel.BasicConsumeAsync(queue: _connection.MultiQueue, autoAck: false, consumer: multiConsumer, stoppingToken);
        _logger.LogInformation("Started consuming from multi queue: {QueueName}", _connection.MultiQueue);
        
        await _connection.Channel.BasicConsumeAsync(queue: _connection.AnyQueue, autoAck: false, consumer: anyConsumer, stoppingToken);
        _logger.LogInformation("Started consuming from any queue: {QueueName}", _connection.AnyQueue);
       
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