using Demo.Domain;
using Demo.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

/// <summary>
/// 메시지 큐에서 수신된 메시지를 처리하는 기본 구현체
/// IMqMessageHandler 인터페이스를 구현하여 비즈니스 로직 처리를 담당합니다
/// </summary>
public class MqMessageHandler : IMqMessageHandler
{
    private readonly ILogger<MqMessageHandler> _logger;

    /// <summary>
    /// MqMessageHandler의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    public MqMessageHandler(ILogger<MqMessageHandler> logger)
    {
        _logger = logger;
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
    public async ValueTask HandleAsync(
        MqSenderType senderType,
        string? sender,
        string? correlationId,
        string? messageId,
        string message,
        CancellationToken ct)
    {
        _logger.LogInformation("Message Processed. {Message}", message);
        await Task.CompletedTask;
    }
}