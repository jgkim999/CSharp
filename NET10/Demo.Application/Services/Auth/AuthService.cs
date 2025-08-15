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
    /// <param name="tracer">텔레메트리를 위한 추적기 (선택사항)</param>
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
    /// <returns>비밀번호가 "admin"이면 true, 그렇지 않으면 false</returns>
    public Task<bool> CredentialsAreValidAsync(string? username, string? password, CancellationToken ct)
    {
        using var span = _tracer?.StartActiveSpan(nameof(AuthService));
        return Task.FromResult(password == "admin");
    }
}