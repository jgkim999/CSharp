using System.Collections.Concurrent;
using FastEndpoints.Security;

namespace GamePulse.Repositories.Jwt;

public class MemoryJwtRepository : IJwtRepository
{
    private static readonly ConcurrentDictionary<string, string> Tokens = new();

    /// <summary>
    ///
    /// </summary>
    /// <param name="response"></param>
    /// <summary>
    /// Stores or updates the refresh token for a user in memory.
    /// </summary>
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
