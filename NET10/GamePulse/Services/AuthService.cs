using OpenTelemetry.Trace;

namespace GamePulse.Services;

/// <summary>
/// 
/// </summary>
public class AuthService : IAuthService
{
    private readonly Tracer? _tracer;

    /// <summary>
    /// 
    /// </summary>
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class with an optional tracer for telemetry.
    /// </summary>
    public AuthService(Tracer? tracer)
    {
        _tracer = tracer;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="ct"></param>
    /// <summary>
    /// Asynchronously determines whether the provided credentials are valid.
    /// </summary>
    /// <param name="username">The username to validate. (Currently unused.)</param>
    /// <param name="password">The password to validate.</param>
    /// <param name="ct">A cancellation token. (Currently unused.)</param>
    /// <returns>True if the password is "admin"; otherwise, false.</returns>
    public Task<bool> CredentialsAreValidAsync(string username, string password, CancellationToken ct)
    {
        using var span = _tracer?.StartActiveSpan(nameof(AuthService));
        return Task.FromResult(password == "admin");
    }
}
