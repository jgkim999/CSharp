namespace Demo.Domain;

/// <summary>
/// 통합된 메시지 큐 서비스 인터페이스 - 발행과 요청-응답 패턴을 모두 지원
/// </summary>
public interface IMqPublishService
{
    ValueTask PublishMultiAsync<T>(
        string exchangeName,
        T messagePack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;

    ValueTask PublishUniqueAsync<T>(
        string queueName,
        T messagePack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;
    
    ValueTask PublishAnyAsync<T>(
        string queueName,
        T messagePack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;
    
    /// <summary>
    /// MessagePack 직렬화된 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<TResponse> PublishAndWaitForResponseAsync<TRequest, TResponse>(
        string queueName,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
        where TRequest : class
        where TResponse : class;
}
