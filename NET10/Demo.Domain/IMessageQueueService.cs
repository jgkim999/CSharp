namespace Demo.Domain;

/// <summary>
/// 통합된 메시지 큐 서비스 인터페이스 - 발행과 요청-응답 패턴을 모두 지원
/// </summary>
public interface IMqPublishService
{
    // 기존 발행 메서드들
    ValueTask PublishMultiAsync(string message, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishMultiAsync(byte[] body, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishMultiAsync<T>(T messagePack, CancellationToken ct = default, string? correlationId = null) where T : class;

    ValueTask PublishUniqueAsync(string target, string message, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishUniqueAsync(string target, byte[] body, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishUniqueAsync<T>(string target, T messagePack, CancellationToken ct = default, string? correlationId = null) where T : class;
    ValueTask PublishAnyAsync(string message, CancellationToken ct = default, string? correlationId = null);
    ValueTask PublishAnyAsync(byte[] body, CancellationToken ct = default, string? correlationId = null);

    // 요청-응답 패턴 메서드들
    /// <summary>
    /// 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<string> SendAndWaitForResponseAsync(string target, string message, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// 바이트 배열 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<byte[]> SendAndWaitForResponseAsync(string target, byte[] body, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// MessagePack 직렬화된 메시지를 보내고 응답을 대기합니다 (타임아웃 지원)
    /// </summary>
    Task<TResponse> SendAndWaitForResponseAsync<TRequest, TResponse>(string target, TRequest request, TimeSpan? timeout = null, CancellationToken ct = default)
        where TRequest : class
        where TResponse : class;
}
