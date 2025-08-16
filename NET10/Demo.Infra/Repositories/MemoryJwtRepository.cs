using System.Collections.Concurrent;
using Demo.Application.Repositories;
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
    /// <param name="response">Token response containing user ID and refresh token</param>
    /// <summary>
    /// Stores or updates the refresh token for the specified user in the in-memory repository.
    /// </summary>
    /// <param name="response">TokenResponse containing the target UserId and the RefreshToken to store.</param>
    /// <returns>A Task that completes once the token has been stored.</returns>
    public async Task StoreTokenAsync(TokenResponse response)
    {
        await Task.CompletedTask;
        Tokens.AddOrUpdate(
            response.UserId,
            response.RefreshToken,
            (key, oldValue) => response.RefreshToken);
    }

    /// <summary>
    /// Validates if the provided refresh token is valid for the specified user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <summary>
    /// Checks whether the provided refresh token matches the stored refresh token for the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user whose stored refresh token will be checked.</param>
    /// <param name="refreshToken">The refresh token to validate against the stored value.</param>
    /// <returns>True if the provided refresh token matches the stored token for the user; otherwise false.</returns>
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