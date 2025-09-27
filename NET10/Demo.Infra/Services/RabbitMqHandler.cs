using System.Diagnostics;
using System.Text;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Domain.Enums;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using MessagePack;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Demo.Infra.Services;

/// <summary>
/// RabbitMQ 메시지 처리를 담당하는 핸들러 클래스
/// OpenTelemetry 추적 컨텍스트를 포함한 메시지 처리 및 응답 관리를 수행합니다
/// </summary>
public class RabbitMqHandler
{
    private readonly RabbitMqConnection _connection;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqMessageHandler _mqMessageHandler;
    private readonly ILogger<RabbitMqHandler> _logger;

    // 타입 캐시 - 성능 최적화를 위해 한번 로드된 타입을 캐시
    private static readonly ConcurrentDictionary<string, Type?> TypeCache = new();

    // MessagePack deserialize 메서드 캐시 - 리플렉션 호출 성능 최적화
    private static readonly ConcurrentDictionary<Type, Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, object?>> DeserializeMethodCache = new();

    /// <summary>
    /// RabbitMqHandler의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="connection">RabbitMQ 연결 인스턴스</param>
    /// <param name="mqMessageHandler">메시지 처리를 위한 핸들러</param>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="telemetryService">텔레메트리 서비스</param>
    public RabbitMqHandler(
        RabbitMqConnection connection,
        IMqMessageHandler mqMessageHandler,
        ILogger<RabbitMqHandler> logger,
        ITelemetryService telemetryService)
    {
        _connection = connection;
        _mqMessageHandler = mqMessageHandler;
        _logger = logger;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// 수신된 메시지의 헤더에서 W3C Trace Context를 파싱하여 OpenTelemetry Activity를 생성합니다
    /// </summary>
    /// <param name="ea">RabbitMQ에서 수신된 메시지 이벤트 인자</param>
    /// <returns>생성된 Activity 또는 null (파싱 실패 시)</returns>
    private Activity? MakeActivity(BasicDeliverEventArgs ea)
    {
        try
        {
            // W3C Trace Context 표준에 따른 traceparent 헤더 파싱
            ActivityContext parentContext = default;
            var traceParentObj = ea.BasicProperties?.Headers?["traceparent"];
            string? traceparent = null;

            if (traceParentObj != null)
            {
                traceparent = traceParentObj switch
                {
                    string str => str,
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    _ => traceParentObj.ToString()
                };
            }

            if (!string.IsNullOrEmpty(traceparent))
            {
                try
                {
                    _logger.LogDebug("Receive traceParent: {TraceParent}", traceparent);
                    // traceparent 형식: version-traceid-spanid-traceflags
                    // 예: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
                    var parts = traceparent.Split('-');
                    if (parts.Length == 4 && parts[0] == "00") // version 00만 지원
                    {
                        var traceId = parts[1];
                        var spanId = parts[2];
                        var traceFlagsStr = parts[3];

                        if (traceId.Length == 32 && spanId.Length == 16 && traceFlagsStr.Length == 2)
                        {
                            var parsedTraceId = ActivityTraceId.CreateFromString(traceId.AsSpan());
                            var parsedSpanId = ActivitySpanId.CreateFromString(spanId.AsSpan());
                            var traceFlags = ActivityTraceFlags.None;
                            if (byte.TryParse(traceFlagsStr, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out var flags))
                            {
                                traceFlags = (ActivityTraceFlags)flags;
                            }
                            else
                            {
                                _logger.LogDebug("Failed to parse trace flags '{TraceFlags}', using default", traceFlagsStr);
                                traceFlags = ActivityTraceFlags.Recorded;
                            }

                            parentContext = new ActivityContext(parsedTraceId, parsedSpanId, traceFlags);
                            _logger.LogDebug("Successfully parsed W3C traceparent: {Traceparent}", traceparent);
                        }
                        else
                        {
                            _logger.LogWarning("Invalid traceparent format lengths: TraceId={TraceIdLength}, SpanId={SpanIdLength}, TraceFlags={TraceFlagsLength}",
                                traceId.Length, spanId.Length, traceFlagsStr.Length);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Unsupported traceparent version or invalid format: {Traceparent}", traceparent);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse W3C traceparent header: {Traceparent}", traceparent);
                }
            }
            return _telemetryService.StartActivity("rabbitmq.handler", ActivityKind.Consumer, parentContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception");
            return null;
        }
    }
    
    /// <summary>
    /// RabbitMQ에서 수신된 메시지를 비동기적으로 처리합니다
    /// W3C Trace Context 파싱, 메시지 디코딩, 비즈니스 로직 처리, ACK/NACK 응답을 수행합니다
    /// </summary>
    /// <param name="senderType">메시지 발송자 타입 (Multi, Any, Unique)</param>
    /// <param name="ea">RabbitMQ에서 수신된 메시지 이벤트 인자</param>
    /// <param name="ct">작업 취소 토큰</param>
    /// <returns>비동기 작업</returns>
    public async ValueTask HandleAsync(MqSenderType senderType, BasicDeliverEventArgs ea, CancellationToken ct = default)
    {
        try
        {
            string exchange = ea.Exchange;
            string routingKey = ea.RoutingKey;
            
            // 메시지 속성에서 Reply-To 정보 추출
            //var appId = ea.BasicProperties?.AppId;
            //var clusterId = ea.BasicProperties?.ClusterId;
            //var contentEncoding = ea.BasicProperties?.ContentEncoding;
            //var contentType = ea.BasicProperties?.ContentType;
            var correlationId = ea.BasicProperties?.CorrelationId;
            //var deliveryMode = ea.BasicProperties?.DeliveryMode;
            //var expiration = ea.BasicProperties?.Expiration;
            //var headers = ea.BasicProperties?.Headers;
            var messageId = ea.BasicProperties?.MessageId;
            //var persistent = ea.BasicProperties?.Persistent;
            //var priority = ea.BasicProperties?.Priority;
            var replyTo = ea.BasicProperties?.ReplyTo;
            //var replyToAddress = ea.BasicProperties?.ReplyToAddress;
            //var timestamp = ea.BasicProperties?.Timestamp.UnixTime;
            //var type = ea.BasicProperties?.Type;
            //var userId = ea.BasicProperties?.UserId;
            
            using var activity = MakeActivity(ea);

            ReadOnlySpan<byte> bodySpan = ea.Body.Span;

            // MessagePack 타입 정보 확인
            var isMessagePack = IsMessagePackMessage(ea.BasicProperties?.Headers);

            _logger.LogDebug(
                "Received message from {QueueType} length: {Length}, Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyTo: {ReplyTo}, CorrelationId: {CorrelationId}, IsMessagePack: {IsMessagePack}",
                senderType, bodySpan.Length, exchange, routingKey, replyTo, correlationId, isMessagePack);

            if (isMessagePack)
            {
                // MessagePack 타입 객체를 직접 처리
                ReadOnlyMemory<byte> bodyArray = ea.Body;
                await ProcessMessagePackMessageAsync(bodyArray, ea.BasicProperties?.Headers, senderType, replyTo, correlationId, messageId, ct);
            }
            else
            {
                var message = Encoding.UTF8.GetString(bodySpan);
                // 일반 문자열 메시지 처리
                var response = await _mqMessageHandler.HandleAsync(senderType, replyTo, correlationId, messageId, message, ct);

                // 응답이 있고 ReplyTo가 있는 경우 응답 전송
                if (!string.IsNullOrEmpty(response) && !string.IsNullOrEmpty(replyTo) && !string.IsNullOrEmpty(correlationId))
                {
                    await SendResponseAsync(replyTo, response, correlationId, ct);
                }
            }
            // 성공적으로 처리된 경우 Ack 전송
            await _connection.Channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
            
            _logger.LogDebug("Message acknowledged for {QueueType} queue, DeliveryTag: {DeliveryTag}",
                senderType, ea.DeliveryTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {QueueType} queue", senderType);
            await _connection.Channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: ct);
        }
    }

    /// <summary>
    /// RabbitMQ 헤더 값을 안전하게 문자열로 변환합니다
    /// RabbitMQ는 헤더 값을 byte[]로 전송하므로 적절한 변환이 필요합니다
    /// </summary>
    /// <param name="headerValue">헤더 값</param>
    /// <returns>변환된 문자열 또는 null</returns>
    private static string? GetHeaderValueAsString(object? headerValue)
    {
        return headerValue switch
        {
            null => null,
            string str => str,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            _ => headerValue.ToString()
        };
    }

    /// <summary>
    /// 메시지 헤더를 확인하여 MessagePack 메시지인지 판단합니다
    /// RabbitMQ 헤더 값이 byte 배열로 전송되는 것을 고려하여 안전하게 처리합니다
    /// </summary>
    /// <param name="headers">메시지 헤더</param>
    /// <returns>MessagePack 메시지 여부</returns>
    private static bool IsMessagePackMessage(IDictionary<string, object?>? headers)
    {
        if (headers == null)
            return false;

        var contentType = GetHeaderValueAsString(headers.TryGetValue("content_type", out var contentTypeValue) ? contentTypeValue : null);
        var messageType = GetHeaderValueAsString(headers.TryGetValue("message_type", out var messageTypeValue) ? messageTypeValue : null);

        return contentType == "application/x-msgpack" && !string.IsNullOrEmpty(messageType);
    }

    /// <summary>
    /// 효율적인 타입 로딩을 위한 헬퍼 메서드
    /// 캐시를 사용하여 이미 로드된 타입을 재사용합니다
    /// 로드된 모든 어셈블리에서 타입을 검색합니다
    /// </summary>
    /// <param name="messageTypeName">타입의 전체 이름</param>
    /// <returns>타입 객체 또는 null</returns>
    private Type? GetTypeFromCache(string messageTypeName)
    {
        return TypeCache.GetOrAdd(messageTypeName, typeName =>
        {
            // 1. 먼저 Type.GetType으로 시도 (mscorlib 및 현재 어셈블리)
            var type = Type.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (type != null)
            {
                _logger.LogDebug("Type {TypeName} found via Type.GetType() and added to cache", typeName);
                return type;
            }

            // 2. 로드된 모든 어셈블리에서 타입 검색
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
                    if (type != null)
                    {
                        _logger.LogDebug("Type {TypeName} found in assembly {AssemblyName} and added to cache",
                            typeName, assembly.GetName().Name);
                        return type;
                    }
                }
                catch (Exception)
                {
                    // 어셈블리 접근 오류 시 무시하고 계속
                }
            }

            _logger.LogWarning("Type {TypeName} not found in any assembly and null cached", typeName);
            return null;
        });
    }

    /// <summary>
    /// MessagePack deserialize 메서드를 컴파일된 델리게이트로 캐시합니다
    /// 리플렉션 호출을 피하여 성능을 크게 향상시킵니다
    /// </summary>
    /// <param name="messageType">deserialize할 메시지 타입</param>
    /// <returns>컴파일된 deserialize 델리게이트</returns>
    private Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, object?> GetDeserializeMethod(Type messageType)
    {
        return DeserializeMethodCache.GetOrAdd(messageType, type =>
        {
            _logger.LogDebug("Creating deserialize method for type {TypeName} and adding to cache", type.FullName);

            // MessagePackSerializer.Deserialize<T>(ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken) 메서드 가져오기
            var deserializeMethod = typeof(MessagePackSerializer)
                .GetMethod("Deserialize", new[] { typeof(ReadOnlyMemory<byte>), typeof(MessagePackSerializerOptions), typeof(CancellationToken) })
                ?.MakeGenericMethod(type);

            if (deserializeMethod == null)
            {
                _logger.LogError("Could not find MessagePackSerializer.Deserialize method for type {TypeName}", type.FullName);
                throw new InvalidOperationException($"Could not find MessagePackSerializer.Deserialize method for type {type.FullName}");
            }

            // Expression Tree를 사용하여 컴파일된 델리게이트 생성
            var bytesParam = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "bytes");
            var optionsParam = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

            var methodCall = Expression.Call(deserializeMethod, bytesParam, optionsParam, cancellationTokenParam);
            var convertToObject = Expression.Convert(methodCall, typeof(object));

            var lambda = Expression.Lambda<Func<ReadOnlyMemory<byte>, MessagePackSerializerOptions, CancellationToken, object?>>(
                convertToObject, bytesParam, optionsParam, cancellationTokenParam);

            var compiledDelegate = lambda.Compile();
            _logger.LogDebug("Successfully compiled deserialize method for type {TypeName} and cached", type.FullName);

            return compiledDelegate;
        });
    }

    /// <summary>
    /// MessagePack 메시지를 처리하여 타입 객체로 deserialize하고 직접 핸들러에 전달합니다
    /// 타입 정보를 헤더에서 읽어와 해당 타입으로 deserialize한 후 핸들러에 전달합니다
    /// </summary>
    /// <param name="bodyArray">메시지 본문</param>
    /// <param name="headers">메시지 헤더</param>
    /// <param name="senderType">메시지 발송자 타입</param>
    /// <param name="sender">메시지 발송자 식별자</param>
    /// <param name="correlationId">메시지 상관 관계 ID</param>
    /// <param name="messageId">메시지 고유 ID</param>
    /// <param name="ct">작업 취소 토큰</param>
    /// <returns>비동기 작업</returns>
    private async ValueTask ProcessMessagePackMessageAsync(
        ReadOnlyMemory<byte> bodyArray,
        IDictionary<string, object?>? headers,
        MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        CancellationToken ct)
    {
        try
        {
            if (headers == null)
            {
                _logger.LogWarning("MessagePack message received but headers are null");
                return;
            }

            var messageTypeName = GetHeaderValueAsString(headers.TryGetValue("message_type", out var messageTypeValue) ? messageTypeValue : null);

            if (string.IsNullOrEmpty(messageTypeName))
            {
                _logger.LogWarning("MessagePack message received but message_type header is missing");
                return;
            }

            // 캐시에서 타입 가져오기 (어셈블리 순회 대신)
            var messageType = GetTypeFromCache(messageTypeName);

            if (messageType == null)
            {
                _logger.LogWarning("Could not resolve type {MessageType}. Available assemblies: {Assemblies}",
                    messageTypeName,
                    string.Join(", ", AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name)));
                return;
            }
          
            // MessagePack deserialize - 컴파일된 델리게이트 사용으로 성능 최적화
            try
            {
                var deserializeFunc = GetDeserializeMethod(messageType);
                var deserializedObject = deserializeFunc(bodyArray, MessagePackSerializerOptions.Standard, ct);

                if (deserializedObject == null)
                {
                    _logger.LogWarning("MessagePack deserialization returned null for type {MessageType}", messageTypeName);
                    return;
                }

                // 타입 객체를 직접 핸들러에 전달
                _logger.LogDebug("Calling HandleMessagePackAsync for type {MessageType}, sender: {Sender}, correlationId: {CorrelationId}",
                    messageType.FullName, sender, correlationId);

                var response = await _mqMessageHandler.HandleMessagePackAsync(senderType, sender, correlationId, messageId, deserializedObject, messageType, ct);

                _logger.LogDebug("HandleMessagePackAsync returned response: {Response} (type: {ResponseType})",
                    response, response?.GetType().FullName ?? "null");

                // 응답이 있고 ReplyTo가 있는 경우 응답 전송
                if (response != null && !string.IsNullOrEmpty(sender) && !string.IsNullOrEmpty(correlationId))
                {
                    _logger.LogInformation("Sending response to {Sender} with correlationId {CorrelationId}", sender, correlationId);
                    await SendResponseAsync(sender, response, correlationId, ct);
                }
                else
                {
                    _logger.LogWarning("No response sent - Response: {Response}, Sender: {Sender}, CorrelationId: {CorrelationId}",
                        response, sender, correlationId);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Could not create deserialize method for type {MessageType}", messageTypeName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MessagePack message");
        }
    }

    /// <summary>
    /// 응답을 RabbitMQ를 통해 전송합니다
    /// </summary>
    /// <param name="replyTo">응답을 보낼 큐 이름</param>
    /// <param name="response">응답 객체 (문자열 또는 객체)</param>
    /// <param name="correlationId">상관 관계 ID</param>
    /// <param name="ct">작업 취소 토큰</param>
    /// <returns>비동기 작업</returns>
    private async ValueTask SendResponseAsync(string replyTo, object response, string correlationId, CancellationToken ct)
    {
        try
        {
            using var span = _telemetryService.StartActivity("rabbitmq.response", ActivityKind.Producer, Activity.Current?.Context);
            span?.SetTag("correlation_id", correlationId);
            span?.SetTag("reply_to", replyTo);

            byte[] responseBody;
            var headers = new Dictionary<string, object>();

            // 응답 타입에 따라 직렬화 방식 결정
            if (response is string stringResponse)
            {
                // 문자열 응답
                responseBody = Encoding.UTF8.GetBytes(stringResponse);
                _logger.LogDebug("Sending string response to {ReplyTo}, Length: {Length}", replyTo, responseBody.Length);
            }
            else
            {
                // 객체 응답 (MessagePack 직렬화)
                using var memoryStream = new MemoryStream();
                await MessagePackSerializer.SerializeAsync(memoryStream, response, cancellationToken: ct);
                responseBody = memoryStream.ToArray();

                // MessagePack 헤더 추가
                headers["content_type"] = "application/x-msgpack";
                headers["message_type"] = response.GetType().FullName ?? response.GetType().Name;
                headers["message_assembly"] = response.GetType().Assembly.GetName().Name ?? string.Empty;

                _logger.LogDebug("Sending MessagePack response to {ReplyTo}, Type: {Type}, Length: {Length}",
                    replyTo, response.GetType().Name, responseBody.Length);
            }

            // 응답 속성 설정
            var properties = new RabbitMQ.Client.BasicProperties
            {
                CorrelationId = correlationId,
                Timestamp = new RabbitMQ.Client.AmqpTimestamp(new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()),
                MessageId = Ulid.NewUlid().ToString(),
                Headers = headers!
            };

            // W3C Trace Context 추가
            if (Activity.Current != null)
            {
                var traceParent = $"00-{Activity.Current.TraceId}-{Activity.Current.SpanId}-{(byte)Activity.Current.ActivityTraceFlags:x2}";
                headers["traceparent"] = traceParent;
                headers["trace_id"] = Activity.Current.TraceId.ToString();
                headers["span_id"] = Activity.Current.SpanId.ToString();
            }

            // 응답 전송 (Default exchange 사용, replyTo를 routing key로 사용)
            await _connection.Channel.BasicPublishAsync(
                exchange: "", // Default exchange
                routingKey: replyTo,
                basicProperties: properties,
                body: responseBody,
                mandatory: false,
                cancellationToken: ct);

            _logger.LogInformation("Response sent successfully to {ReplyTo} with CorrelationId: {CorrelationId}",
                replyTo, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send response to {ReplyTo} with CorrelationId: {CorrelationId}",
                replyTo, correlationId);
        }
    }
}