using FastEndpoints;
using FastEndpoints.Security;
using Bogus;
using Demo.Application.DTO;
using Demo.Application.Processors;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Infra.Services;
using System.Diagnostics;

namespace GamePulse.EndPoints.Login;

public class LoginEndpointV1 : Endpoint<LoginRequest, TokenResponse>
{
    private readonly IAuthService _authService;
    private readonly ILogger<LoginEndpointV1> _logger;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginEndpointV1"/> class with the specified authentication service, logger, and telemetry service.
    /// </summary>
    /// <param name="authService">인증 서비스</param>
    /// <param name="logger">로거</param>
    /// <param name="telemetryService">텔레메트리 서비스</param>
    public LoginEndpointV1(IAuthService authService, ILogger<LoginEndpointV1> logger, ITelemetryService telemetryService)
    {
        _logger = logger;
        _authService = authService;
        _telemetryService = telemetryService;
    }

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
    /// 사용자 로그인을 처리하고 JWT 토큰을 반환합니다
    /// </summary>
    /// <param name="req">로그인 요청</param>
    /// <param name="ct">취소 토큰</param>
    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // OpenTelemetry Activity 시작
        using var activity = _telemetryService.StartActivity("user.login", new Dictionary<string, object?>
        {
            ["user.name"] = req.Username,
            ["endpoint.name"] = "LoginEndpointV1",
            ["endpoint.version"] = "v1"
        });

        try
        {
            // 구조화된 로깅 with 트레이스 컨텍스트
            _telemetryService.LogInformationWithTrace(_logger,
                "사용자 로그인 시도: {Username}", req.Username);

            // 인증 검증
            var isValidCredentials = await _authService.CredentialsAreValidAsync(req.Username, req.Password, ct);

            if (!isValidCredentials)
            {
                // 실패 메트릭 기록
                _telemetryService.RecordError("authentication_failed", "user.login", "Invalid credentials");

                // Activity에 에러 정보 설정
                activity?.SetTag("auth.result", "failed");
                activity?.SetTag("error.type", "authentication_failed");

                _telemetryService.LogWarningWithTrace(_logger,
                    "로그인 실패: 잘못된 자격 증명 - {Username}", req.Username);

                ThrowError("Invalid username and password");
            }

            // 성공적인 인증 후 토큰 생성
            var faker = new Faker();
            var userId = faker.Random.Uuid().ToString();

            Response = await CreateTokenWith<MyTokenService>(userId, u =>
            {
                u.Roles.AddRange(new [] {"Admin", "Manager"});
                u.Permissions.Add("Write");
                u.Claims.Add(new ("UserName", req.Username));
                u.Claims.Add(new ("UserId", userId));
            });

            // 성공 메트릭 및 Activity 설정
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;

            _telemetryService.RecordHttpRequest("POST", "/api/login", 200, duration);
            _telemetryService.RecordBusinessMetric("login_success", 1, new Dictionary<string, object?>
            {
                ["user.name"] = req.Username,
                ["user.id"] = userId
            });

            activity?.SetTag("auth.result", "success");
            activity?.SetTag("user.id", userId);
            activity?.SetTag("http.status_code", 200);

            _telemetryService.SetActivitySuccess(activity, "User login successful");

            _telemetryService.LogInformationWithTrace(_logger,
                "사용자 로그인 성공: {Username}, UserId: {UserId}, Duration: {Duration}ms",
                req.Username, userId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // 예외 처리 및 메트릭 기록
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;

            _telemetryService.RecordHttpRequest("POST", "/api/login", 500, duration);
            _telemetryService.RecordError("login_exception", "user.login", ex.Message);
            _telemetryService.SetActivityError(activity, ex);

            _telemetryService.LogErrorWithTrace(_logger, ex,
                "로그인 처리 중 예외 발생: {Username}", req.Username);

            throw; // 예외를 다시 던져서 FastEndpoints가 처리하도록 함
        }
    }
}
