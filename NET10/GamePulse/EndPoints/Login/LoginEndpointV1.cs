using FastEndpoints;
using FastEndpoints.Security;
using GamePulse.DTO;
using GamePulse.Services;
using Bogus;
using GamePulse.Processors;
using OpenTelemetry.Trace;

namespace GamePulse.EndPoints.Login;

/// <summary>
/// 
/// </summary>
public class LoginEndpointV1 : Endpoint<LoginRequest, TokenResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginEndpointV1> _logger;
    private readonly Tracer _tracer;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authService"></param>
    /// <param name="logger"></param>
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginEndpointV1"/> class with the specified authentication service, logger, and tracer.
    /// </summary>
    public LoginEndpointV1(IAuthService authService, ILogger<LoginEndpointV1> logger, Tracer tracer)
    {
        _logger = logger;
        _authService = authService;
        _tracer = tracer;
    }
    
    /// <summary>
    /// 
    /// <summary>
    /// Configures the login endpoint with versioning, route, anonymous access, validation error logging, rate limiting, and API documentation summary.
    /// </summary>
    public override void Configure()
    {
        Version(1);
        Post("/api/login");
        AllowAnonymous();
        
        PreProcessor<ValidationErrorLogger<LoginRequest>>();
        // 헤더 이름은 원하는 대로 설정할 수 있습니다.
        // 지정하지 않으면 라이브러리는 들어오는 요청에서 X-Forwarded-For 헤더의 값을 읽으려고 시도합니다.
        // 실패하면 요청을 하는 클라이언트를 고유하게 식별하기 위해 HttpContext.Connection.RemoteIpAddress 를 읽으려고 시도합니다.
        // 모든 시도가 실패하면 403 Forbidden 응답이 전송됩니다.
        // https://fast-endpoints.com/docs/rate-limiting#header-name
        Throttle(
            hitLimit: 60,
            durationSeconds: 60);
        Summary(s => 
        {
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
