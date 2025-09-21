using Demo.Application.DTO;
using Demo.Domain;
using Demo.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.Frozen;

namespace Demo.Application.Services;

/// <summary>
/// 메시지 큐에서 수신된 메시지를 처리하는 기본 구현체
/// IMqMessageHandler 인터페이스를 구현하여 비즈니스 로직 처리를 담당합니다
/// </summary>
public class MqMessageHandler : IMqMessageHandler
{
    private readonly ILogger<MqMessageHandler> _logger;
    private readonly FrozenDictionary<
        string,
        Func<MqSenderType, string?, string?, string?, object, Type, CancellationToken, ValueTask>> _handlers;
    
    /// <summary>
    /// MqMessageHandler의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    public MqMessageHandler(ILogger<MqMessageHandler> logger)
    {
        _logger = logger;
        
        _handlers = new Dictionary<string, Func<MqSenderType, string?, string?, string?, object, Type, CancellationToken, ValueTask>>(2)
        {
            { typeof(MqPublishRequest).FullName!, OnMqPublishRequestAsync },
            { typeof(MqPublishRequest2).FullName!, OnMqPublishRequest2Async }
        }.ToFrozenDictionary();
    }

    private ValueTask OnMqPublishRequest2Async(MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        object messageObject,
        Type messageType,
        CancellationToken ct)
    {
        try
        {
            if (messageObject is not MqPublishRequest2 request)
            {
                _logger.LogError("Message casting error. MessageId: {MessageId}, ExpectedType: {ExpectedType}, ActualType: {ActualType}",
                    messageId, nameof(MqPublishRequest2), messageObject.GetType().Name);
                return ValueTask.CompletedTask;
            }
            _logger.LogInformation("Message Processed. MessageId: {MessageId}, CreatedAt: {CreatedAt}", messageId, request.CreatedAt);
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MqPublishRequest2. MessageId: {MessageId}", messageId);
            return ValueTask.CompletedTask;
        }
    }

    private ValueTask OnMqPublishRequestAsync(
        MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        object messageObject,
        Type messageType,
        CancellationToken ct)
    {
        try
        {
            if (messageObject is not MqPublishRequest request)
            {
                _logger.LogError("Message casting error. MessageId: {MessageId}, ExpectedType: {ExpectedType}, ActualType: {ActualType}",
                    messageId, nameof(MqPublishRequest), messageObject.GetType().Name);
                return ValueTask.CompletedTask;
            }
            _logger.LogInformation("Message Processed. MessageId: {MessageId}, Message: {Message}", messageId, request.Message);
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MqPublishRequest. MessageId: {MessageId}", messageId);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 메시지 큐에서 수신된 메시지를 비동기적으로 처리합니다
    /// 현재는 로깅만 수행하는 기본 구현이며, 실제 비즈니스 로직으로 확장 가능합니다
    /// </summary>
    /// <param name="senderType">메시지 발송자 타입 (Multi, Any, Unique)</param>
    /// <param name="sender">메시지 발송자 식별자</param>
    /// <param name="correlationId">메시지 상관 관계 ID</param>
    /// <param name="messageId">메시지 고유 ID</param>
    /// <param name="message">처리할 메시지 내용</param>
    /// <param name="ct">작업 취소 토큰</param>
    /// <returns>비동기 작업</returns>
    public ValueTask HandleAsync(
        MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        string message,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Message Processed. MessageId: {MessageId}, Message: {Message}", messageId, message);
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message. MessageId: {MessageId}", messageId);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// MessagePack으로 deserialize된 타입 객체를 직접 처리합니다
    /// 타입별로 다른 처리 로직을 구현할 수 있습니다
    /// </summary>
    /// <param name="senderType">메시지 발송자 타입 (Multi, Any, Unique)</param>
    /// <param name="sender">메시지 발송자 식별자</param>
    /// <param name="correlationId">메시지 상관 관계 ID</param>
    /// <param name="messageId">메시지 고유 ID</param>
    /// <param name="messageObject">deserialize된 메시지 객체</param>
    /// <param name="messageType">메시지 객체의 타입</param>
    /// <param name="ct">작업 취소 토큰</param>
    /// <returns>비동기 작업</returns>
    public async ValueTask HandleMessagePackAsync(
        MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        object messageObject,
        Type messageType,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "MessagePack Object Processed. MessageId: {MessageId}, Type: {MessageType}",
                messageId, messageType.FullName);

            if (_handlers.TryGetValue(messageType.FullName!, out var handler))
            {
                await handler(senderType, sender, correlationId, messageId, messageObject, messageType, ct);
            }
            else
            {
                _logger.LogWarning("No handler registered for message type: {MessageType}, MessageId: {MessageId}",
                    messageType.FullName, messageId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MessagePack object. MessageId: {MessageId}, Type: {MessageType}",
                messageId, messageType.FullName);
        }
    }
}
