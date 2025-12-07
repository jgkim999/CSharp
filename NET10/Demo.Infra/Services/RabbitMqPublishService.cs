using System.Text;
using Demo.Domain;
using Demo.Domain.Constants;
using Demo.Infra.Configs;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Demo.Application.Services;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using Demo.Application.Utils;
using Demo.Domain.Enums;
using MessagePack;
using Microsoft.IO;

namespace Demo.Infra.Services;

public sealed class RabbitMqPublishService : IMqPublishService, IDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly RabbitMqHandler _handler;
    
    private readonly string _uniqueQueue;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<RabbitMqPublishService> _logger;

    // 요청-응답 매칭을 위한 대기 중인 요청 저장소
    private readonly ConcurrentDictionary<string, TaskCompletionSource<(string? StringResponse, byte[]? ByteResponse)>> _pendingRequests;
    private readonly AsyncEventingBasicConsumer _uniqueConsumer;

    // 메모리 최적화를 위한 객체 풀링
    private readonly ConcurrentBag<StringBuilder> _stringBuilderPool;

    // RecyclableMemoryStream 관리자
    private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new ();

    // AssemblyName 캐시 - Type별 어셈블리 이름을 캐싱하여 반복 할당 방지
    private static readonly ConcurrentDictionary<Type, string> AssemblyNameCache = new();
    
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
        
        _connection = connection;
        _logger = logger;
        _telemetryService = telemetryService;
        _handler = handler;

        // 요청-응답 매칭용 딕셔너리 초기화 (성능 최적화)
        _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<(string?, byte[]?)>>(
            Environment.ProcessorCount, 100);

        // 메모리 최적화를 위한 객체 풀 초기화
        _stringBuilderPool = new ConcurrentBag<StringBuilder>();

        // 초기 StringBuilder들을 미리 생성하여 풀에 추가
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            _stringBuilderPool.Add(new StringBuilder(64)); // traceparent 길이 고려
        }

        // Unique queue 생성 (메시지를 받기 위한 고유 queue)
        _uniqueQueue = connection.UniqueQueue;
        
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
    }

    private void Dispose(bool disposing)
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="body"></param>
    /// <param name="messageType"></param>
    /// <param name="contentType">
    /// MessagePack = "application/x-msgpack"
    /// Protocol Buffers = "application/x-protobuf"
    /// </param>
    /// <param name="correlationId"></param>
    /// <returns></returns>
    private (ReadOnlyMemory<byte> Body, BasicProperties Properties) MakeDataWithType(
        ReadOnlyMemory<byte> body,
        Type? messageType,
        string contentType,
        string? correlationId = null)
    {
        // 헤더는 매번 새로 생성 (RabbitMQ 메시지와 함께 전송되므로 풀링 불가)
        var headers = new Dictionary<string, object>(8);

        // MessagePack 타입 정보를 헤더에 추가
        if (messageType != null)
        {
            headers["message_type"] = messageType.FullName ?? messageType.Name;
            // AssemblyName 캐싱을 통한 메모리 최적화
            headers["message_assembly"] = AssemblyNameCache.GetOrAdd(messageType,
                t => t.Assembly.GetName().Name ?? string.Empty);
            headers["content_type"] = contentType;
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

    public async ValueTask PublishMultiAsync<T>(
        string role,
        MqBinaryType binaryType,
        T message,
        CancellationToken ct = default,
        string? correlationId = null) where T : class?
    {
        string exchangeName = MqName.MultiExchange(role);
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // RecyclableMemoryStream을 사용한 메모리 최적화 (동기 Dispose이므로 using 사용)
        await using var memoryStream = MemoryStreamManager.GetStream();

        string contentType;

        switch (binaryType)
        {
            case MqBinaryType.MessagePack:
                await MessagePackSerializer.SerializeAsync(memoryStream, message, cancellationToken: ct);
                contentType = ContentTypes.MessagePack;
                break;
            case MqBinaryType.Protobuf:
                ProtoBuf.Serializer.Serialize(memoryStream, message);
                contentType = ContentTypes.ProtoBuf;
                break;
            case MqBinaryType.MemoryPack:
                await MemoryPack.MemoryPackSerializer.SerializeAsync(memoryStream, message, cancellationToken: ct);
                contentType = ContentTypes.MemoryPack;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(binaryType), binaryType, null);
        }
        
        // GetBuffer()와 Length를 사용하여 불필요한 배열 복사 제거
        var buffer = memoryStream.GetBuffer();
        var serializedMemory = buffer.AsMemory(0, (int)memoryStream.Length);
        var data = MakeDataWithType(serializedMemory, typeof(T), contentType, correlationId);
        await PublishMultiAsync(exchangeName, data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishMultiAsync(
        string exchangeName,
        ReadOnlyMemory<byte> body,
        BasicProperties properties,
        CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity("rabbitmq.publish.multi", ActivityKind.Producer, Activity.Current?.Context);
        await _connection.Channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: "", // fanout에서는 routing key 불필요
            basicProperties: properties,
            body: body,
            mandatory: false,
            cancellationToken: ct);
        _logger.LogDebug("Multi message sent: CorrelationId: {CorrelationId}", properties.CorrelationId);
    }

    public async ValueTask PublishAnyAsync<T>(
        string role,
        MqBinaryType binaryType,
        T message,
        CancellationToken ct = default,
        string? correlationId = null) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        string queueName = MqName.AnyQueue(role);
        // RecyclableMemoryStream을 사용한 메모리 최적화
        await using var memoryStream = MemoryStreamManager.GetStream();

        string contentType;
        
        switch (binaryType)
        {
            case MqBinaryType.MessagePack:
                await MessagePackSerializer.SerializeAsync(memoryStream, message, cancellationToken: ct);
                contentType = ContentTypes.MessagePack;
                break;
            case MqBinaryType.Protobuf:
                ProtoBuf.Serializer.Serialize(memoryStream, message);
                contentType = ContentTypes.ProtoBuf;
                break;
            case MqBinaryType.MemoryPack:
                await MemoryPack.MemoryPackSerializer.SerializeAsync(memoryStream, message, cancellationToken: ct);
                contentType = ContentTypes.MemoryPack;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(binaryType), binaryType, null);
        }
        // GetBuffer()와 Length를 사용하여 불필요한 배열 복사 제거
        var buffer = memoryStream.GetBuffer();
        var serializedMemory = buffer.AsMemory(0, (int)memoryStream.Length);
        var data = MakeDataWithType(serializedMemory, typeof(T), contentType, correlationId);
        await PublishAnyAsync(queueName, data.Body, data.Properties, ct);
    }

    private async ValueTask PublishAnyAsync(
        string queueName,
        ReadOnlyMemory<byte> body,
        BasicProperties properties,
        CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity("rabbitmq.publish.any", ActivityKind.Producer, Activity.Current?.Context);
        // Round-robin: Direct exchange를 사용하여 동일한 routing key로 바인딩된 모든 consumer가 round-robin으로 처리
        await _connection.Channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName, // 모든 Any queue가 이 routing key로 바인딩됨
            basicProperties: properties,
            body: body,
            mandatory: false,
            cancellationToken: ct);
        _logger.LogDebug(
            "Any message sent (round-robin): CorrelationId: {CorrelationId}",
            properties.CorrelationId);
    }
    
    public async Task<TResponse> PublishAnyAndWaitForResponseAsync<TRequest, TResponse>(
        string role,
        TRequest request,
        MqBinaryType binaryType,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string? correlationId = null) where TRequest : class where TResponse : class
    {
        var messageCorrelationId = correlationId ?? Ulid.NewUlid().ToString();
        
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var timeoutSpan = timeout ?? TimeSpan.FromSeconds(30);
            
            var tcs = new TaskCompletionSource<(string?, byte[]?)>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[messageCorrelationId] = tcs;
            
            await PublishAnyAsync(
                role,
                binaryType,
                request,
                ct,
                messageCorrelationId);
            
            // 응답 대기 (타임아웃 지원)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeoutSpan);

            var responseTask = tcs.Task;
            var timeoutTask = Task.Delay(timeoutSpan, timeoutCts.Token);

            var completedTask = await Task.WhenAny(responseTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning(
                    "Request timeout for correlation ID: {CorrelationId}, Target: {Target}",
                    messageCorrelationId, MqName.AnyQueue(role));
                throw new TimeoutException(
                    $"Request timeout after {timeoutSpan.TotalSeconds} seconds for queueName: {MqName.AnyQueue(role)}");
            }

            var (_, byteResponse) = await responseTask;

            if (byteResponse == null)
            {
                throw new InvalidOperationException("Received null response");
            }

            // 역직렬화
            TResponse? response;
            switch (binaryType)
            {
                case MqBinaryType.MessagePack:
                    response = MessagePackSerializer.Deserialize<TResponse>(
                        byteResponse, cancellationToken: timeoutCts.Token);
                    break;
                case MqBinaryType.Protobuf:
                    response = ProtoBuf.Serializer.Deserialize<TResponse>(byteResponse);
                    break;
                case MqBinaryType.MemoryPack:
                    response = MemoryPack.MemoryPackSerializer.Deserialize<TResponse>(byteResponse.AsSpan());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binaryType), binaryType, null);
            }
            
            _logger.LogDebug("Response received and deserialized for correlation ID: {CorrelationId}", messageCorrelationId);

            return response ?? throw new InvalidOperationException("Deserialized response is null");
        }
        finally
        {
            _pendingRequests.TryRemove(messageCorrelationId, out _);
        }
    }
    
    public async Task PublishUniqueAsync<T>(
        string uniqueQueueName,
        T message,
        MqBinaryType binaryType,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string? correlationId = null)
        where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // RecyclableMemoryStream을 사용한 메모리 최적화
        await using var memoryStream = MemoryStreamManager.GetStream();
        
        string contentType;
        
        switch (binaryType)
        {
            case MqBinaryType.MessagePack:
                await MessagePackSerializer.SerializeAsync(memoryStream, message, cancellationToken: ct);
                contentType = ContentTypes.MessagePack;
                break;
            case MqBinaryType.Protobuf:
                ProtoBuf.Serializer.Serialize(memoryStream, message);
                contentType = ContentTypes.ProtoBuf;
                break;
            case MqBinaryType.MemoryPack:
                await MemoryPack.MemoryPackSerializer.SerializeAsync(memoryStream, message, cancellationToken: ct);
                contentType = ContentTypes.MemoryPack;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(binaryType), binaryType, null);
        }
        
        // GetBuffer()와 Length를 사용하여 불필요한 배열 복사 제거
        var buffer = memoryStream.GetBuffer();
        var serializedMemory = buffer.AsMemory(0, (int)memoryStream.Length);
        var data = MakeDataWithType(serializedMemory, typeof(T), contentType, correlationId);
        await PublishUniqueAsync(uniqueQueueName, data.Body, data.Properties, ct);
    }
    
    private async ValueTask PublishUniqueAsync(
        string queueName,
        ReadOnlyMemory<byte> body,
        BasicProperties properties,
        CancellationToken ct = default)
    {
        using var span = _telemetryService.StartActivity("rabbitmq.publish.unique", ActivityKind.Producer, Activity.Current?.Context);
        // 특정 Consumer의 Reply queue로 직접 전송 (exchange 없이)
        await _connection.Channel.BasicPublishAsync(
            exchange: "", // Default exchange 사용 (queue 이름을 routing key로 사용)
            routingKey: queueName,
            basicProperties: properties,
            body: body,
            mandatory: false,
            cancellationToken: ct);
        _logger.LogDebug(
            "Unique Message sent. Target: {Target}, CorrelationId: {CorrelationId}",
            queueName, properties.CorrelationId);
    }

    public async Task<TResponse> PublishUniqueAndWaitForResponseAsync<TRequest, TResponse>(
        string uniqueQueueName,
        TRequest request,
        MqBinaryType binaryType,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string? correlationId = null)
        where TRequest : class
        where TResponse : class
    {
        var messageCorrelationId = correlationId ?? Ulid.NewUlid().ToString();
        
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var timeoutSpan = timeout ?? TimeSpan.FromSeconds(30);
            
            var tcs = new TaskCompletionSource<(string?, byte[]?)>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[messageCorrelationId] = tcs;
            
            await PublishUniqueAsync(
                uniqueQueueName,
                request,
                binaryType,
                timeoutSpan,
                ct,
                messageCorrelationId);
            
            // 응답 대기 (타임아웃 지원)
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeoutSpan);

            var responseTask = tcs.Task;
            var timeoutTask = Task.Delay(timeoutSpan, timeoutCts.Token);

            var completedTask = await Task.WhenAny(responseTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning(
                    "Request timeout for correlation ID: {CorrelationId}, Target: {Target}",
                    messageCorrelationId, uniqueQueueName);
                throw new TimeoutException(
                    $"Request timeout after {timeoutSpan.TotalSeconds} seconds for queueName: {uniqueQueueName}");
            }

            var (_, byteResponse) = await responseTask;

            if (byteResponse == null)
            {
                throw new InvalidOperationException("Received null response");
            }

            // 역직렬화
            TResponse? response;
            switch (binaryType)
            {
                case MqBinaryType.MessagePack:
                    response = MessagePackSerializer.Deserialize<TResponse>(
                        byteResponse, cancellationToken: timeoutCts.Token);
                    break;
                case MqBinaryType.Protobuf:
                    response = ProtoBuf.Serializer.Deserialize<TResponse>(byteResponse);
                    break;
                case MqBinaryType.MemoryPack:
                    response = MemoryPack.MemoryPackSerializer.Deserialize<TResponse>(byteResponse.AsSpan());
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(binaryType), binaryType, null);
            }
            
            _logger.LogDebug("Response received and deserialized for correlation ID: {CorrelationId}", messageCorrelationId);

            return response ?? throw new InvalidOperationException("Deserialized response is null");
        }
        finally
        {
            _pendingRequests.TryRemove(messageCorrelationId, out _);
        }
    }
    
    private async Task OnUniqueReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var correlationId = ea.BasicProperties.CorrelationId;

            _logger.LogDebug("Received message in unique queue. CorrelationId: {CorrelationId}, HasHeaders: {HasHeaders}",
                correlationId, ea.BasicProperties.Headers != null);

            // 요청-응답 처리 확인
            if (!string.IsNullOrEmpty(correlationId) && _pendingRequests.TryGetValue(correlationId, out var tcs))
            {
                // 응답 타입 확인 (헤더 기반) - RabbitMQ 헤더는 byte[]로 전송됨
                var isBinaryMessage = false;
                if (ea.BasicProperties.Headers?.TryGetValue("content_type", out var contentTypeObj) == true)
                {
                    var contentType = contentTypeObj switch
                    {
                        string str => str,
                        byte[] bytes => Encoding.UTF8.GetString(bytes),
                        _ => contentTypeObj?.ToString()
                    };
                    // MessagePack, ProtoBuf, MemoryPack 응답 모두 바이너리로 처리
                    isBinaryMessage = contentType == ContentTypes.MessagePack
                        || contentType == ContentTypes.ProtoBuf
                        || contentType == ContentTypes.MemoryPack;

                    _logger.LogDebug("Response content type: {ContentType}, IsBinaryMessage: {IsBinaryMessage}",
                        contentType, isBinaryMessage);
                }

                if (isBinaryMessage)
                {
                    // 바이너리 응답 처리 (MessagePack 또는 ProtoBuf)
                    var responseBytes = ea.Body.ToArray();
                    _logger.LogDebug("Processing binary response, bytes length: {Length}", responseBytes.Length);
                    tcs.TrySetResult((null, responseBytes));
                }
                else
                {
                    // 문자열 응답 처리
                    var responseText = Encoding.UTF8.GetString(ea.Body.Span);
                    _logger.LogDebug("Processing string response: {Response}", responseText);
                    tcs.TrySetResult((responseText, null));
                }

                _logger.LogInformation("Response processed for correlation ID: {CorrelationId}", correlationId);
            }
            else
            {
                _logger.LogDebug("No pending request found for correlation ID: {CorrelationId}. Processing as regular message.", correlationId);
                // 일반 메시지 처리 (기존 로직)
                await _handler.HandleAsync(MqSenderType.Unique, ea);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing reply message");
        }
    }
}
