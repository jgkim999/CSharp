using Demo.Domain.Enums;

namespace Demo.Domain;

/// <summary>
/// 통합된 메시지 큐 서비스 인터페이스 - 발행과 요청-응답 패턴을 모두 지원
/// </summary>
public interface IMqPublishService
{
    /// <summary>
    /// Multi
    /// Exchange에 연결된 모든 Queue에 전달 
    /// Exchange -> Queue1 -> Consumer1 (1) (2) (3)
    ///          -> Queue2 -> Consumer2 (1) (2) (3)
    ///          -> Queue3 -> Consumer3 (1) (2) (3)
    /// </summary>
    /// <param name="role"></param>
    /// <param name="binaryType"></param>
    /// <param name="message"></param>
    /// <param name="ct"></param>
    /// <param name="correlationId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask PublishMultiAsync<T>(
        string role,
        MqBinaryType binaryType,
        T message,
        CancellationToken ct = default,
        string? correlationId = null) where T : class?;
    
    /// <summary>
    /// Any
    /// Queue에 연결된 Consumer에 라운드로빈 방식으로 전달
    /// Queue1 -> Consumer1 (1) (4)
    ///       -> Consumer2 (2) (5)
    ///       -> Consumer3 (3) (6)
    /// </summary>
    /// <param name="role"></param>
    /// <param name="binaryType"></param>
    /// <param name="message"></param>
    /// <param name="ct"></param>
    /// <param name="correlationId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    ValueTask PublishAnyAsync<T>(
        string role,
        MqBinaryType binaryType,
        T message,
        CancellationToken ct = default,
        string? correlationId = null) where T : class;
    
    /// <summary>
    /// 직렬화된 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<TResponse> PublishAnyAndWaitForResponseAsync<TRequest, TResponse>(
        string role,
        TRequest request,
        MqBinaryType binaryType,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string? correlationId = null)
        where TRequest : class
        where TResponse : class;
    
    /// <summary>
    /// Unique
    /// 정확한 Queue이름으로 전달
    /// Queue1 -> Consumer1
    /// Queue2 -> Consumer2
    /// Queue3 -> Consumer3
    /// </summary>
    /// <param name="uniqueQueueName"></param>
    /// <param name="message"></param>
    /// <param name="binaryType"></param>
    /// <param name="timeout"></param>
    /// <param name="ct"></param>
    /// <param name="correlationId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task PublishUniqueAsync<T>(
        string uniqueQueueName,
        T message,
        MqBinaryType binaryType,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string? correlationId = null)
        where T : class;
    
    /// <summary>
    /// Unique
    /// 정확한 Queue이름으로 전달
    /// Queue1 -> Consumer1
    /// Queue2 -> Consumer2
    /// Queue3 -> Consumer3
    /// </summary>
    /// <param name="uniqueQueueName"></param>
    /// <param name="request"></param>
    /// <param name="binaryType"></param>
    /// <param name="timeout"></param>
    /// <param name="ct"></param>
    /// <param name="correlationId"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    Task<TResponse> PublishUniqueAndWaitForResponseAsync<TRequest, TResponse>(
        string uniqueQueueName,
        TRequest request,
        MqBinaryType binaryType,
        TimeSpan? timeout = null,
        CancellationToken ct = default,
        string? correlationId = null)
        where TRequest : class
        where TResponse : class;
}
