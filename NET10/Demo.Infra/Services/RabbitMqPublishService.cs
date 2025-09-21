using System.Text;
using Demo.Domain;
using Demo.Infra.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Demo.Application.Services;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Demo.Infra.Services;

public class RabbitMqPublishService : IMqPublishService, IDisposable
{
    private readonly RabbitMqConfig _config;
    private readonly RabbitMqConnection _connection;
    
    private readonly string _uniqueQueue;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<RabbitMqPublishService> _logger;

    // 요청-응답 매칭을 위한 대기 중인 요청 저장소
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests;
    private readonly AsyncEventingBasicConsumer _uniqueConsumer;
   
    public RabbitMqPublishService(
        IOptions<RabbitMqConfig> config,
        RabbitMqConnection connection,
        ITelemetryService telemetryService,
        ILogger<RabbitMqPublishService> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(telemetryService);
        ArgumentNullException.ThrowIfNull(logger);
        _config = config.Value;
        _connection = connection;
        _logger = logger;
        _telemetryService = telemetryService;

        // 요청-응답 매칭용 딕셔너리 초기화
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        // Unique queue 생성 (메시지를 받기 위한 고유 queue)
        _uniqueQueue = $"{Environment.MachineName}.unique.{Ulid.NewUlid()}";
        _connection.Channel.QueueDeclareAsync(
            queue: _uniqueQueue,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null);

        // Unique consumer 설정
        _uniqueConsumer = new AsyncEventingBasicConsumer(_connection.Channel);
        _uniqueConsumer.ReceivedAsync += OnUniqueReceived;

        // Unique queue에서 메시지 수신 시작
        _connection.Channel.BasicConsumeAsync(
            queue: _uniqueQueue,
            autoAck: true,
            consumer: _uniqueConsumer);

        // Exchange들은 이미 RabbitMqConnection에서 선언됨
        // - ProducerExchangeMulti: fanout 타입
        // - ProducerExchangeAny: direct 타입
    }
    
    public void Dispose()
    {
        _connection.Dispose();
    }

    private (byte[] Body, BasicProperties Properties) MakeData(string message, string? correlationId = null)
    {
        var body = Encoding.UTF8.GetBytes(message);
        return MakeData(body, correlationId);
    }
    
    private (byte[] Body, BasicProperties Properties) MakeData(byte[] body, string? correlationId = null)
    {
        var properties = new BasicProperties
        {
            ReplyTo = _uniqueQueue,
            CorrelationId = correlationId ?? Ulid.NewUlid().ToString(),
            Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
            MessageId = Guid.NewGuid().ToString(),
            Headers = new Dictionary<string, object>
            {
                ["trace_id"] = Activity.Current?.TraceId.ToString() ?? "",
                ["span_id"] = Activity.Current?.SpanId.ToString() ?? ""
            }!
        };
        return (body, properties);
    }
    
    public async ValueTask PublishUniqueAsync(
        string target, string message, CancellationToken ct = default, string? correlationId = null)
    {
        var data = MakeData(message, correlationId);
        await PublishUniqueAsync(target, data.Body, data.Properties, ct);
    }
    
    public async ValueTask PublishUniqueAsync(
        string target, byte[] message, CancellationToken ct = default, string? correlationId = null)
    {
        var data = MakeData(message, correlationId);
        await PublishUniqueAsync(target, data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishUniqueAsync(
        string target, byte[] body, BasicProperties properties, CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity(nameof(PublishUniqueAsync), ActivityKind.Producer, Activity.Current?.Context);
        // 특정 Consumer의 Reply queue로 직접 전송 (exchange 없이)
        await _connection.Channel.BasicPublishAsync(
            exchange: "", // Default exchange 사용 (queue 이름을 routing key로 사용)
            routingKey: target,
            basicProperties: properties,
            body: body,
            mandatory: false,
            cancellationToken: ct);
        _logger.LogDebug(
            "Unique Message sent. Target: {Target}, CorrelationId: {CorrelationId}",
            target, properties.CorrelationId);
    }

    public async ValueTask PublishMultiAsync(
        string message, CancellationToken ct = default, string? correlationId = null)
    {
        var data = MakeData(message, correlationId);
        await PublishMultiAsync(data.Body, data.Properties, ct);
    }
    
    public async ValueTask PublishMultiAsync(
        byte[] body, CancellationToken ct = default, string? correlationId = null)
    {
        var data = MakeData(body, correlationId);
        await PublishMultiAsync(data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishMultiAsync(
        byte[] body, BasicProperties properties, CancellationToken ct = default, string? correlationId = null)
    {
        using var span = _telemetryService.StartActivity(nameof(PublishMultiAsync), ActivityKind.Producer, Activity.Current?.Context);
        await _connection.Channel.BasicPublishAsync(
            exchange: _connection.ProducerExchangeMulti,
            routingKey: "", // fanout에서는 routing key 불필요
            basicProperties: properties,
            body: body,
            mandatory: false,
            cancellationToken: ct);
        _logger.LogDebug("Multi message sent: CorrelationId: {CorrelationId}", properties.CorrelationId);
    }

    public async ValueTask PublishAnyAsync(
        string message, CancellationToken ct = default, string? correlationId = null)
    {
        var data = MakeData(message, correlationId);
        await PublishAnyAsync(data.Body, data.Properties, ct);
    }

    public async ValueTask PublishAnyAsync(
        byte[] body, CancellationToken ct = default, string? correlationId = null)
    {
        var data = MakeData(body, correlationId);
        await PublishAnyAsync(data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishAnyAsync(
        byte[] body, BasicProperties properties, CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity(nameof(PublishAnyAsync), ActivityKind.Producer, Activity.Current?.Context);
        // Round-robin: Direct exchange를 사용하여 동일한 routing key로 바인딩된 모든 consumer가 round-robin으로 처리
        await _connection.Channel.BasicPublishAsync(
            exchange: "",
            routingKey: _connection.AnyQueue, // 모든 Any consumer가 이 routing key로 바인딩됨
            basicProperties: properties,
            body: body,
            mandatory: false,
            cancellationToken: ct);
        _logger.LogDebug(
            "Any message sent (round-robin): CorrelationId: {CorrelationId}",
            properties.CorrelationId);
    }

    private async Task OnUniqueReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var correlationId = ea.BasicProperties?.CorrelationId;
            var responseBody = ea.Body.ToArray();
            var traceId = ea.BasicProperties?.Headers?["trace_id"]?.ToString();
            var spanId = ea.BasicProperties?.Headers?["span_id"]?.ToString();
         
            using var span = _telemetryService.StartActivity(nameof(OnUniqueReceived), ActivityKind.Consumer, traceId);
            
            var responseMessage = Encoding.UTF8.GetString(responseBody);

            _logger.LogInformation(
                "Unique received. CorrelationId: {CorrelationId}, Message: {Message}",
                correlationId, responseMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reply message");
        }
        await Task.CompletedTask;
    }
}
