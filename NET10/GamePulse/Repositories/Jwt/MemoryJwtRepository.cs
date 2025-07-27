using System.Collections.Concurrent;
using FastEndpoints.Security;

namespace GamePulse.Repositories.Jwt;

/// <summary>
/// 
/// </summary>
public class MemoryJwtRepository : IJwtRepository
{
    private static readonly ConcurrentDictionary<string, string> Tokens = new();
    
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    public async Task StoreTokenAsync(TokenResponse response)
    {
        await Task.CompletedTask;
        Tokens.AddOrUpdate(
            response.UserId,
            response.RefreshToken,
            (key, oldValue) => response.RefreshToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<bool> TokenIsValidAsync(string userId, string refreshToken)
    {
        await Task.CompletedTask;
        if (Tokens.TryGetValue(userId, out var value))
        {
            return value == refreshToken;
        }
        return false;
    }
}
