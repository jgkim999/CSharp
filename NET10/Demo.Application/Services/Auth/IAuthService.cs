namespace Demo.Application.Services.Auth;

/// <summary>
/// 인증 서비스 인터페이스
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 사용자 자격 증명의 유효성을 비동기적으로 검증합니다
    /// </summary>
    /// <param name="username">검증할 사용자명 (null 허용)</param>
    /// <param name="password">검증할 비밀번호 (null 허용)</param>
    /// <param name="ct">취소 토큰</param>
    /// <summary>
/// Asynchronously verifies whether the provided username and password are valid.
/// </summary>
/// <param name="username">The username to validate; may be null.</param>
/// <param name="password">The password to validate; may be null.</param>
/// <param name="ct">A cancellation token to cancel the validation operation.</param>
/// <returns>A task that resolves to true if the credentials are valid; otherwise false.</returns>
    Task<bool> CredentialsAreValidAsync(string? username, string? password, CancellationToken ct);
}