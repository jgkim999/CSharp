using FastEndpoints.Security;

namespace Demo.Application.Repositories;

/// <summary>
/// Interface for JWT token repository operations
/// </summary>
public interface IJwtRepository
{
    /// <summary>
    /// Stores a JWT token response for future validation
    /// </summary>
    /// <param name="response">Token response containing user ID and refresh token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task StoreTokenAsync(TokenResponse response);

    /// <summary>
    /// Validates if the provided refresh token is valid for the specified user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    Task<bool> TokenIsValidAsync(string userId, string refreshToken);
}