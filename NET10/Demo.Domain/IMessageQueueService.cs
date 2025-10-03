namespace Demo.Domain;

/// <summary>
/// 통합된 메시지 큐 서비스 인터페이스 - 발행과 요청-응답 패턴을 모두 지원
/// </summary>
public interface IMqPublishService
{
    ValueTask PublishMessagePackMultiAsync<T>(
        string exchangeName,
        T messagePack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;

    ValueTask PublishProtoBufMultiAsync<T>(
        string exchangeName,
        T protoBuf,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;

    ValueTask PublishMemoryPackMultiAsync<T>(
        string exchangeName,
        T memoryPack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;

    ValueTask PublishMessagePackUniqueAsync<T>(
        string queueName,
        T messagePack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;
    
    ValueTask PublishMessagePackAnyAsync<T>(
        string queueName,
        T messagePack,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;
    
    /// <summary>
    /// MessagePack 직렬화된 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<TResponse> PublishMessagePackAndWaitForResponseAsync<TRequest, TResponse>(
        string queueName,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// ProtoBuf 직렬화된 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<TResponse> PublishProtoBufAndWaitForResponseAsync<TRequest, TResponse>(
        string queueName,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
        where TRequest : class
        where TResponse : class;

    /// <summary>
    /// MemoryPack 직렬화된 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<TResponse> PublishMemoryPackAndWaitForResponseAsync<TRequest, TResponse>(
        string queueName,
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
        where TRequest : class
        where TResponse : class;
}
