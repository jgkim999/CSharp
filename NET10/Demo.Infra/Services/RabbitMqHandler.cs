using System.Diagnostics;
using System.Text;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Domain.Enums;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

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
            var message = Encoding.UTF8.GetString(bodySpan);

            _logger.LogDebug(
                "Received message from {QueueType} length: {Length}, Exchange: {Exchange}, RoutingKey: {RoutingKey}, ReplyTo: {ReplyTo}, CorrelationId: {CorrelationId}",
                senderType, bodySpan.Length, exchange, routingKey, replyTo, correlationId);

            // 메시지 처리 로직 (실제 비즈니스 로직은 여기에 구현)
            await _mqMessageHandler.HandleAsync(senderType, replyTo, correlationId, messageId, message, ct);
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
}