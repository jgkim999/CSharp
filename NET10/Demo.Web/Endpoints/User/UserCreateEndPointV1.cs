using Demo.Application.Commands;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using System.Diagnostics;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    private readonly TelemetryService _telemetryService;
    private readonly ILogger<UserCreateEndpointV1> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreateEndpointV1"/> class with the specified command mediator and telemetry service.
    /// </summary>
    public UserCreateEndpointV1(
        ICommandMediator commandMediator, 
        TelemetryService telemetryService,
        ILogger<UserCreateEndpointV1> logger)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }
    
    /// <summary>
    /// Configures the endpoint to handle HTTP POST requests at the route "/api/user/create" and allows anonymous access.
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();
    }

    /// <summary>
    /// Processes a user creation request by sending a command to create a new user and responds with the appropriate HTTP status based on the result.
    /// </summary>
    /// <param name="req">The user creation request containing name, email, and password.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    public override async Task HandleAsync(UserCreateRequest req, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // 사용자 정의 Activity 시작
        using var activity = _telemetryService.StartActivity("user.create", new Dictionary<string, object?>
        {
            ["user.email"] = req.Email,
            ["user.name"] = req.Name,
            ["operation.type"] = "user_creation",
            ["endpoint.name"] = "UserCreateEndpointV1",
            ["endpoint.version"] = "v1"
        });

        try
        {
            // 요청 시작 로그 (트레이스 컨텍스트 포함)
            TelemetryService.LogInformationWithTrace(_logger, 
                "사용자 생성 요청 시작 - Email: {Email}, Name: {Name}", req.Email, req.Name);

            // 비즈니스 메트릭 기록 - 요청 시작
            _telemetryService.RecordBusinessMetric("user_creation_attempts", 1, new Dictionary<string, object?>
            {
                ["endpoint"] = "user_create_v1",
                ["operation"] = "start"
            });

            // 명령 실행
            var ret = await _commandMediator.SendAsync(
                new UserCreateCommand(req.Name, req.Email, req.Password),
                cancellationToken: ct);

            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;

            if (ret.Result.IsFailed)
            {
                // 실패 처리
                var errorMessages = ret.Result.Errors.Select(e => e.Message).ToList();
                var errorMessage = string.Join(", ", errorMessages);

                // Activity에 에러 정보 설정
                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, errorMessage);
                    activity.SetTag("error", true);
                    activity.SetTag("error.type", "UserCreationFailed");
                    activity.SetTag("error.message", errorMessage);
                    activity.SetTag("error.count", ret.Result.Errors.Count);
                }

                // 에러 메트릭 기록
                _telemetryService.RecordError("UserCreationFailed", "user_create_v1", errorMessage);

                // 실패 비즈니스 메트릭 기록
                _telemetryService.RecordBusinessMetric("user_creation_failures", 1, new Dictionary<string, object?>
                {
                    ["endpoint"] = "user_create_v1",
                    ["error_type"] = "validation_failed",
                    ["error_count"] = ret.Result.Errors.Count
                });

                // HTTP 요청 메트릭 기록
                _telemetryService.RecordHttpRequest("POST", "/api/user/create", 500, duration);

                // 에러 로그 (트레이스 컨텍스트 포함)
                TelemetryService.LogErrorWithTrace(_logger, 
                    new InvalidOperationException(errorMessage),
                    "사용자 생성 실패 - Email: {Email}, Errors: {ErrorMessage}", req.Email, errorMessage);

                // FastEndpoints 에러 응답
                foreach (var error in ret.Result.Errors)
                {
                    AddError(error.Message);
                }
                await Send.ErrorsAsync(500, ct);
            }
            else
            {
                // 성공 처리
                TelemetryService.SetActivitySuccess(activity, "사용자가 성공적으로 생성되었습니다");

                // 성공 비즈니스 메트릭 기록
                _telemetryService.RecordBusinessMetric("user_creation_successes", 1, new Dictionary<string, object?>
                {
                    ["endpoint"] = "user_create_v1",
                    ["operation"] = "success"
                });

                // HTTP 요청 메트릭 기록
                _telemetryService.RecordHttpRequest("POST", "/api/user/create", 200, duration);

                // 성공 로그 (트레이스 컨텍스트 포함)
                TelemetryService.LogInformationWithTrace(_logger, 
                    "사용자 생성 성공 - Email: {Email}, Duration: {Duration}ms", req.Email, duration * 1000);

                await Send.OkAsync(cancellation: ct);
            }

            // 처리 시간 메트릭 기록
            _telemetryService.RecordBusinessMetric("user_creation_duration_ms", (long)(duration * 1000), new Dictionary<string, object?>
            {
                ["endpoint"] = "user_create_v1",
                ["success"] = ret.Result.IsSuccess.ToString().ToLower()
            });

            // Activity에 처리 시간 추가
            activity?.SetTag("duration.ms", duration * 1000);
            activity?.SetTag("success", ret.Result.IsSuccess);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed.TotalSeconds;

            // Activity에 예외 정보 설정
            TelemetryService.SetActivityError(activity, ex);

            // 예외 메트릭 기록
            _telemetryService.RecordError("UnhandledException", "user_create_v1", ex.Message);

            // 예외 비즈니스 메트릭 기록
            _telemetryService.RecordBusinessMetric("user_creation_exceptions", 1, new Dictionary<string, object?>
            {
                ["endpoint"] = "user_create_v1",
                ["exception_type"] = ex.GetType().Name
            });

            // HTTP 요청 메트릭 기록
            _telemetryService.RecordHttpRequest("POST", "/api/user/create", 500, duration);

            // 예외 로그 (트레이스 컨텍스트 포함)
            TelemetryService.LogErrorWithTrace(_logger, ex, 
                "사용자 생성 중 예외 발생 - Email: {Email}", req.Email);

            // 예외 재발생
            throw;
        }
    }
}
