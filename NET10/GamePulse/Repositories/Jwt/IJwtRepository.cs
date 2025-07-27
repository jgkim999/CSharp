using FastEndpoints.Security;

namespace GamePulse.Repositories.Jwt;

/// <summary>
/// 
/// </summary>
public interface IJwtRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    Task StoreTokenAsync(TokenResponse response);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    Task<bool> TokenIsValidAsync(string userId, string refreshToken);
}
