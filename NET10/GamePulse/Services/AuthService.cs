using OpenTelemetry.Trace;

namespace GamePulse.Services;

/// <summary>
/// 
/// </summary>
public class AuthService : IAuthService
{
    private readonly Tracer _tracer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tracer"></param>
    public AuthService(Tracer tracer)
    {
        _tracer = tracer;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public Task<bool> CredentialsAreValidAsync(string username, string password, CancellationToken ct)
    {
        using var span = _tracer.StartActiveSpan(nameof(AuthService));
        return Task.FromResult(password == "admin");
    }
}
