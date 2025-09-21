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
using Demo.Domain.Enums;
using System.Buffers;
using MessagePack;
using Microsoft.IO;

namespace Demo.Infra.Services;

public class RabbitMqPublishService : IMqPublishService, IDisposable
{
    private readonly RabbitMqConfig _config;
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqHandler _handler;
    
    private readonly string _uniqueQueue;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<RabbitMqPublishService> _logger;

    // 요청-응답 매칭을 위한 대기 중인 요청 저장소
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests;
    private readonly AsyncEventingBasicConsumer _uniqueConsumer;

    // 메모리 최적화를 위한 객체 풀링
    private readonly ConcurrentBag<StringBuilder> _stringBuilderPool;

    // RecyclableMemoryStream 관리자
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager =
        new RecyclableMemoryStreamManager();
   
    public RabbitMqPublishService(
        IOptions<RabbitMqConfig> config,
        RabbitMqConnection connection,
        RabbitMqHandler handler,
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
        _handler = handler;

        // 요청-응답 매칭용 딕셔너리 초기화 (성능 최적화)
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<string>>(
            Environment.ProcessorCount, 100);

        // 메모리 최적화를 위한 객체 풀 초기화
        _stringBuilderPool = new ConcurrentBag<StringBuilder>();

        // 초기 StringBuilder들을 미리 생성하여 풀에 추가
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            _stringBuilderPool.Add(new StringBuilder(64)); // traceparent 길이 고려
        }

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
    
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try
                {
                    // Consumer 이벤트 핸들러 해제
                    _uniqueConsumer.ReceivedAsync -= OnUniqueReceived;

                    // 대기 중인 요청들 정리 (타임아웃 처리)
                    foreach (var pendingRequest in _pendingRequests.Values)
                    {
                        pendingRequest.TrySetCanceled();
                    }
                    _pendingRequests.Clear();

                    // StringBuilder 풀 정리 (GC가 처리하도록)
                    while (_stringBuilderPool.TryTake(out _))
                    {
                        // StringBuilder 객체들을 풀에서 제거
                    }

                    // Connection 해제
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during RabbitMqPublishService disposal");
                }
            }
            _disposed = true;
        }
    }


    /// <summary>
    /// StringBuilder를 풀에서 가져오거나 새로 생성합니다
    /// </summary>
    private StringBuilder GetStringBuilder()
    {
        if (_stringBuilderPool.TryTake(out var sb))
        {
            sb.Clear();
            return sb;
        }
        return new StringBuilder(64);
    }

    /// <summary>
    /// StringBuilder를 풀에 반환합니다
    /// </summary>
    private void ReturnStringBuilder(StringBuilder sb)
    {
        if (sb.Capacity <= 256) // 너무 큰 StringBuilder는 풀에 반환하지 않음
        {
            _stringBuilderPool.Add(sb);
        }
    }

    private (ReadOnlyMemory<byte> Body, BasicProperties Properties) MakeData(string message, string? correlationId = null)
    {
        // ArrayPool을 사용한 메모리 최적화
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(message.Length);
        var rental = ArrayPool<byte>.Shared.Rent(maxByteCount);
        try
        {
            var actualLength = Encoding.UTF8.GetBytes(message, rental);
            var body = new byte[actualLength];
            Array.Copy(rental, body, actualLength);
            return MakeData(body.AsMemory(), correlationId);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rental);
        }
    }
    
    private (ReadOnlyMemory<byte> Body, BasicProperties Properties) MakeData(ReadOnlyMemory<byte> body, string? correlationId = null)
    {
        return MakeDataWithType(body, null, correlationId);
    }

    private (ReadOnlyMemory<byte> Body, BasicProperties Properties) MakeDataWithType(ReadOnlyMemory<byte> body, Type? messageType, string? correlationId = null)
    {
        // 헤더는 매번 새로 생성 (RabbitMQ 메시지와 함께 전송되므로 풀링 불가)
        var headers = new Dictionary<string, object>(8);

        // MessagePack 타입 정보를 헤더에 추가
        if (messageType != null)
        {
            headers["message_type"] = messageType.FullName ?? messageType.Name;
            headers["message_assembly"] = messageType.Assembly.GetName().Name ?? string.Empty;
            headers["content_type"] = "application/x-msgpack";
        }

        var properties = new BasicProperties
        {
            ReplyTo = _uniqueQueue,
            CorrelationId = correlationId ?? Ulid.NewUlid().ToString(),
            Timestamp = new AmqpTimestamp(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
            MessageId = Ulid.NewUlid().ToString(),
            Headers = headers!
        };

        // W3C Trace Context 표준에 따른 traceparent 헤더 추가
        if (Activity.Current != null)
        {
            // StringBuilder를 사용한 문자열 연결 최적화
            var sb = GetStringBuilder();
            try
            {
                // ToString()을 사용하되 StringBuilder로 최적화
                var traceId = Activity.Current.TraceId.ToString();
                var spanId = Activity.Current.SpanId.ToString();
                var traceFlagsValue = (byte)Activity.Current.ActivityTraceFlags;

                // StringBuilder로 traceparent 구성 (메모리 할당 최소화)
                sb.Append("00-")
                  .Append(traceId)
                  .Append('-')
                  .Append(spanId)
                  .Append('-')
                  .Append(traceFlagsValue.ToString("x2"));

                var traceParent = sb.ToString();

                // 로깅을 Debug 레벨로 변경 (프로덕션 성능 최적화)
                _logger.LogDebug("Send traceParent: {TraceParent}", traceParent);

                headers["traceparent"] = traceParent;
                headers["trace_id"] = traceId;
                headers["span_id"] = spanId;
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        return (body, properties);
    }

    public async ValueTask PublishUniqueAsync(
        string target, string message, CancellationToken ct = default, string? correlationId = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = MakeData(message, correlationId);
        await PublishUniqueAsync(target, data.Body, data.Properties, ct);
    }
    
    public async ValueTask PublishUniqueAsync(
        string target, byte[] message, CancellationToken ct = default, string? correlationId = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = MakeData(message.AsMemory(), correlationId);
        await PublishUniqueAsync(target, data.Body, data.Properties, ct);
    }

    public async ValueTask PublishUniqueAsync<T>(string target, T messagePack, CancellationToken ct = default, string? correlationId = null) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // RecyclableMemoryStream을 사용한 메모리 최적화
        using var memoryStream = MemoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, messagePack, cancellationToken: ct);

        // ReadOnlyMemory로 직접 사용하여 불필요한 배열 복사 제거
        var serializedMemory = memoryStream.GetReadOnlySequence().IsSingleSegment
            ? memoryStream.GetReadOnlySequence().FirstSpan.ToArray().AsMemory()
            : memoryStream.ToArray().AsMemory();
        var data = MakeDataWithType(serializedMemory, typeof(T), correlationId);
        await PublishUniqueAsync(target, data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishUniqueAsync(
        string target, ReadOnlyMemory<byte> body, BasicProperties properties, CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity("rabbitmq.publish.unique", ActivityKind.Producer, Activity.Current?.Context);
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = MakeData(message, correlationId);
        await PublishMultiAsync(data.Body, data.Properties, ct);
    }
    
    public async ValueTask PublishMultiAsync(
        byte[] body, CancellationToken ct = default, string? correlationId = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = MakeData(body.AsMemory(), correlationId);
        await PublishMultiAsync(data.Body, data.Properties, ct);
    }
    
    public async ValueTask PublishMultiAsync<T>(T messagePack, CancellationToken ct = default, string? correlationId = null) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // RecyclableMemoryStream을 사용한 메모리 최적화
        using var memoryStream = MemoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, messagePack, cancellationToken: ct);

        // ReadOnlyMemory로 직접 사용하여 불필요한 배열 복사 제거
        var serializedMemory = memoryStream.GetReadOnlySequence().IsSingleSegment
            ? memoryStream.GetReadOnlySequence().FirstSpan.ToArray().AsMemory()
            : memoryStream.ToArray().AsMemory();
        var data = MakeDataWithType(serializedMemory, typeof(T), correlationId);
        await PublishMultiAsync(data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishMultiAsync(
        ReadOnlyMemory<byte> body, BasicProperties properties, CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity("rabbitmq.publish.multi", ActivityKind.Producer, Activity.Current?.Context);
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
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = MakeData(message, correlationId);
        await PublishAnyAsync(data.Body, data.Properties, ct);
    }

    public async ValueTask PublishAnyAsync(
        byte[] body, CancellationToken ct = default, string? correlationId = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var data = MakeData(body.AsMemory(), correlationId);
        await PublishAnyAsync(data.Body, data.Properties, ct);
    }

    public async ValueTask PublishAnyAsync<T>(T messagePack, CancellationToken ct = default, string? correlationId = null) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // RecyclableMemoryStream을 사용한 메모리 최적화
        using var memoryStream = MemoryStreamManager.GetStream();
        await MessagePackSerializer.SerializeAsync(memoryStream, messagePack, cancellationToken: ct);

        // ReadOnlyMemory로 직접 사용하여 불필요한 배열 복사 제거
        var serializedMemory = memoryStream.GetReadOnlySequence().IsSingleSegment
            ? memoryStream.GetReadOnlySequence().FirstSpan.ToArray().AsMemory()
            : memoryStream.ToArray().AsMemory();
        var data = MakeDataWithType(serializedMemory, typeof(T), correlationId);
        await PublishAnyAsync(data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishAnyAsync(
        ReadOnlyMemory<byte> body, BasicProperties properties, CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity("rabbitmq.publish.any", ActivityKind.Producer, Activity.Current?.Context);
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
            await _handler.HandleAsync(MqSenderType.Unique, ea);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reply message");
        }
    }
}
