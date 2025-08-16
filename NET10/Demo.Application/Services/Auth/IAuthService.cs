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
    /// <returns>자격 증명이 유효하면 true, 그렇지 않으면 false</returns>
    Task<bool> CredentialsAreValidAsync(string? username, string? password, CancellationToken ct);
}