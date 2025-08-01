namespace GamePulse.Services.Auth;

/// <summary>
/// 
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    Task<bool> CredentialsAreValidAsync(string username, string password, CancellationToken ct);
}
