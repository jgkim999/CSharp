using FastEndpoints;
using FastEndpoints.Security;
using GamePulse.DTO;
using GamePulse.Services;
using Bogus;

namespace GamePulse.EndPoints.Login;

/// <summary>
/// 
/// </summary>
public class UserLoginEndpoint_v1 : Endpoint<LoginRequest>
{
    private readonly IAuthService _authService;
    private readonly ILogger<UserLoginEndpoint_v1> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="authService"></param>
    /// <param name="logger"></param>
    public UserLoginEndpoint_v1(IAuthService authService, ILogger<UserLoginEndpoint_v1> logger)
    {
        _logger = logger;
        _authService = authService;
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
        _logger.LogInformation("{@Request}", req);
        
        if (await _authService.CredentialsAreValidAsync(req.Username, req.Password, ct) == false)
        {
            ThrowError("Invalid username and password");
            return;
        }

        var jwtToken = JwtBearer.CreateToken(o =>
        {
            var faker = new Faker();
            o.ExpireAt = DateTime.UtcNow.AddDays(1);
            o.User.Roles.Add("Manager", "Auditor");
            o.User.Claims.Add(("UserName", req.Username));
            o.User["UserId"] = faker.Hashids.EncodeLong(faker.Random.Long(1, long.MaxValue));
        });

        await Send.OkAsync(
            new
            {
                req.Username,
                Token = jwtToken
            });
    }
}
