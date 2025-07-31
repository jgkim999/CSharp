using FastEndpoints;
using FluentValidation;

namespace GamePulse.DTO;

/// <summary>
/// Login 요청
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 로그인 사용자명
    /// </summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>
    /// 로그인 비밀번호
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
