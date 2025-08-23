using System.Collections.Concurrent;
using Demo.Domain.Repositories;
using FastEndpoints.Security;

namespace Demo.Infra.Repositories;

/// <summary>
/// In-memory implementation of JWT token repository for development and testing purposes
/// </summary>
public class MemoryJwtRepository : IJwtRepository
{
    private static readonly ConcurrentDictionary<string, string> Tokens = new();

    /// <summary>
    /// Stores a JWT token response in memory
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="refreshToken">Refresh token to store</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task StoreTokenAsync(string userId, string refreshToken)
    {
        await Task.CompletedTask;
        Tokens.AddOrUpdate(
            userId,
            refreshToken,
            (key, oldValue) => refreshToken);
    }

    /// <summary>
    /// Stores a JWT token response in memory (backward compatibility)
    /// </summary>
    /// <param name="response">Token response containing user ID and refresh token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task StoreTokenAsync(TokenResponse response)
    {
        await StoreTokenAsync(response.UserId, response.RefreshToken);
    }

    /// <summary>
    /// Validates if the provided refresh token is valid for the specified user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
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