using FastEndpoints;
using FastEndpoints.Security;
using GamePulse.DTO;
using GamePulse.Services;
using Bogus;
using OpenTelemetry.Trace;

namespace GamePulse.EndPoints.Login;

/// <summary>
/// 
/// </summary>
public class Endpoint_v1 : Endpoint<LoginRequest, TokenResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<Endpoint_v1> _logger;
    private readonly Tracer _tracer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authService"></param>
    /// <param name="logger"></param>
    /// <param name="tracer"></param>
    public Endpoint_v1(IAuthService authService, ILogger<Endpoint_v1> logger, Tracer tracer)
    {
        _logger = logger;
        _authService = authService;
        _tracer = tracer;
    }
    
    /// <summary>
    /// 
    /// </summary>
    public override void Configure()
    {
        Version(1);
        Post("/api/login");
        AllowAnonymous();
        Summary(s => {
            s.Summary = "사용자 로그인";
            s.Description = "입력한 정보를 바탕으로 로그인을 수행합니다.";
            s.Response<MyResponse>(200, "Login successfully");
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        using var span = _tracer.StartActiveSpan("Login");
        _logger.LogInformation("{@Request}", req);
        
        if (await _authService.CredentialsAreValidAsync(req.Username, req.Password, ct) == false)
        {
            ThrowError("Invalid username and password");
        }
        
        var faker = new Faker();
        var userId = faker.Random.Uuid().ToString();
        Response = await CreateTokenWith<MyTokenService>(userId, u =>
        {
            u.Roles.AddRange(new [] {"Admin", "Manager"});
            u.Permissions.Add("Write");
            u.Claims.Add(new ("UserName", req.Username));
            u.Claims.Add(new ("UserId", userId));
        });
    }
}
