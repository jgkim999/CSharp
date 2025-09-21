using System.Diagnostics;
using System.Text;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Domain.Enums;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;

namespace Demo.Infra.Services;

public class RabbitMqHandler
{
    private readonly RabbitMqConnection _connection;
    private readonly ITelemetryService _telemetryService;
    private readonly IMqMessageHandler _mqMessageHandler;
    private readonly ILogger<RabbitMqHandler> _logger;
    
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
    
    public async ValueTask HandleAsync(MqSenderType senderType, BasicDeliverEventArgs ea, CancellationToken ct = default)
    {
        try
        {
            string exchange = ea.Exchange;
            string routingKey = ea.RoutingKey;
            
            // 메시지 속성에서 Reply-To 정보 추출
            var replyTo = ea.BasicProperties?.ReplyTo;
            var correlationId = ea.BasicProperties?.CorrelationId;
            var messageId = ea.BasicProperties?.MessageId;
            //var contentType = ea.BasicProperties?.ContentType;
            //var contentEncoding = ea.BasicProperties?.ContentEncoding;
            //var priority = ea.BasicProperties?.Priority;
            //var expiration = ea.BasicProperties?.Expiration;
            //var userId = ea.BasicProperties?.UserId;
            //var appId = ea.BasicProperties?.AppId;
            //var clusterId = ea.BasicProperties?.ClusterId;
            //var type = ea.BasicProperties?.Type;
            //var timestamp = ea.BasicProperties?.Timestamp.UnixTime;
            //var headers = ea.BasicProperties?.Headers;
            //var deliveryMode = ea.BasicProperties?.DeliveryMode;
            //var persistent = ea.BasicProperties?.Persistent;
            //var correlationIdObj = ea.BasicProperties?.CorrelationId;
            //var replyToAddress = ea.BasicProperties?.ReplyToAddress;
            
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

            using var activity = _telemetryService.StartActivity("RabbitMqConsumerService.ProcessMessageAsync", ActivityKind.Consumer, parentContext);

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