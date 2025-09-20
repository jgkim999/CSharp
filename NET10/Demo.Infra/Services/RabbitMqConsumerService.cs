using Demo.Infra.Configs;
using System.Text;
using Demo.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Demo.Domain;

namespace Demo.Infra.Services;

public class RabbitMqConsumerService : BackgroundService
{
    private readonly RabbitMqConnection _connection;
    private readonly ILogger<RabbitMqConsumerService> _logger;
    
    private readonly string _multiQueue;

    public RabbitMqConsumerService(
        IOptions<RabbitMqConfig> config,
        RabbitMqConnection connection,
        ILogger<RabbitMqConsumerService> logger,
        ITelemetryService telemetryService,
        IMqPublishService publishService)
    {
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(publishService);
        _logger = logger;
        _connection = connection;

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
            await ProcessMessageAsync("Multi", ea, stoppingToken);
        };

        // Any queue consumer
        var anyConsumer = new AsyncEventingBasicConsumer(_connection.Channel);
        anyConsumer.ReceivedAsync += async (model, ea) =>
        {
            await ProcessMessageAsync("Any", ea, stoppingToken);
        };

        // 3개 queue에서 메시지 수신 시작 (autoAck: false로 수동 Ack 설정)
        await _connection.Channel.BasicConsumeAsync(queue: _multiQueue, autoAck: false, consumer: multiConsumer, stoppingToken);
        await _connection.Channel.BasicConsumeAsync(queue: _connection.AnyQueue, autoAck: false, consumer: anyConsumer, stoppingToken);
        
        // 무한 대기하며 메시지 처리
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(string queueType, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        try
        {
            string exchange = ea.Exchange;
            string routingKey = ea.RoutingKey;

            ReadOnlySpan<byte> bodySpan = ea.Body.Span;
            var message = Encoding.UTF8.GetString(bodySpan);

            // 메시지 속성에서 Reply-To 정보 추출
            var replyTo = ea.BasicProperties?.ReplyTo;
            var correlationId = ea.BasicProperties?.CorrelationId;
            var messageId = ea.BasicProperties?.MessageId;

            _logger.LogInformation(
                "Received message from {QueueType} queue: {Message}, Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyTo: {ReplyTo}, CorrelationId: {CorrelationId}",
                queueType, message, exchange, routingKey, replyTo, correlationId);

            // 메시지 처리 로직 (실제 비즈니스 로직은 여기에 구현)
            var responseMessage = await ProcessBusinessLogicAsync(queueType, message, stoppingToken);
            
            // 성공적으로 처리된 경우 Ack 전송
            await _connection.Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

            _logger.LogDebug("Message acknowledged for {QueueType} queue, DeliveryTag: {DeliveryTag}",
                queueType, ea.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {QueueType} queue", queueType);

            // 처리 실패 시 Nack (requeue: true로 재큐잉)
            await _connection.Channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task<string> ProcessBusinessLogicAsync(string queueType, string message, CancellationToken stoppingToken)
    {
        // 실제 비즈니스 로직 구현
        // 각 queue 타입별로 다른 처리 로직을 구현할 수 있습니다
        return queueType switch
        {
            "Multi" => await ProcessMultiQueueMessage(message, stoppingToken),
            "Any" => await ProcessAnyQueueMessage(message, stoppingToken),
            _ => $"Unknown queue type: {queueType}"
        };
    }

    private async Task<string> ProcessMultiQueueMessage(string message, CancellationToken stoppingToken)
    {
        // Multi queue 메시지 처리 로직
        _logger.LogInformation("Processing Multi queue message: {Message}", message);
        await Task.Delay(100, stoppingToken); // 시뮬레이션

        return $"Multi processed: {message} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }

    private async Task<string> ProcessAnyQueueMessage(string message, CancellationToken stoppingToken)
    {
        // Any queue 메시지 처리 로직
        _logger.LogInformation("Processing Any queue message: {Message}", message);
        await Task.Delay(100, stoppingToken); // 시뮬레이션

        return $"Any processed: {message} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
    }
}