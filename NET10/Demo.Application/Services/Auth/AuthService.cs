using OpenTelemetry.Trace;

namespace Demo.Application.Services.Auth;

/// <summary>
/// 인증 서비스 구현체
/// </summary>
public class AuthService : IAuthService
{
    private readonly Tracer? _tracer;

    /// <summary>
    /// 텔레메트리를 위한 선택적 추적기와 함께 AuthService 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <summary>
    /// Initializes a new instance of <see cref="AuthService"/>.
    /// </summary>
    /// <remarks>
    /// Optionally accepts an OpenTelemetry <see cref="Tracer"/>; when non-null the service will create active spans for its operations. Pass <c>null</c> to disable tracing.
    /// </remarks>
    public AuthService(Tracer? tracer)
    {
        _tracer = tracer;
    }

    /// <summary>
    /// 제공된 자격 증명이 유효한지 비동기적으로 확인합니다
    /// </summary>
    /// <param name="username">검증할 사용자명 (현재 사용되지 않음, null 허용)</param>
    /// <param name="password">검증할 비밀번호 (null 허용)</param>
    /// <param name="ct">취소 토큰 (현재 사용되지 않음)</param>
    /// <summary>
    /// Validates credentials and returns true only when the provided password equals "admin".
    /// </summary>
    /// <param name="username">The username (not used by this implementation).</param>
    /// <param name="password">The password to validate; comparison is performed against the literal "admin".</param>
    /// <param name="ct">Cancellation token (not observed by this implementation).</param>
    /// <returns>True if <paramref name="password"/> is exactly "admin"; otherwise false.</returns>
    /// <remarks>
    /// If a Tracer was provided to the service, an OpenTelemetry span with the name of the service is started for the call.
    /// The method completes synchronously and returns a completed <see cref="Task{TResult}"/>.
    /// </remarks>
    public Task<bool> CredentialsAreValidAsync(string? username, string? password, CancellationToken ct)
    {
        using var span = _tracer?.StartActiveSpan(nameof(AuthService));
        return Task.FromResult(password == "admin");
    }
}