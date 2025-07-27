namespace GamePulse.Services;

/// <summary>
/// 
/// </summary>
public class AuthService : IAuthService
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<bool> CredentialsAreValidAsync(string username, string password, CancellationToken ct)
    {
        return Task.FromResult(password == "admin");
    }
}
