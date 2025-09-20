using Demo.Application.Commands;
using Demo.Application.Configs;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using Demo.Application.Extensions;
using Demo.Application.Models;
using Microsoft.Extensions.Options;

namespace Demo.Web.Endpoints.User;

/// <summary>
/// 
/// </summary>
public class UserCreateEndpointSummary : Summary<UserCreateEndpointV1>
{
    public UserCreateEndpointSummary()
    {
        Summary = "새로운 사용자를 생성합니다.";
        Description = "이 엔드포인트는 사용자 이름과 이메일 주소를 받아 시스템에 새로운 사용자를 등록하고, 생성된 사용자의 ID를 반환합니다.";
        ExampleRequest = new UserCreateRequest { Name = "John Doe", Email = "john.doe@example.com", Password = "1234qwer!@#$"};
        // HTTP 응답 코드별 설명 추가
        //Response<MyResponse>(200, "ok response with body", example: new() {...});
        //Response<ErrorResponse>(400, "validation failure");
        //Response(404, "account not found");
    }
}

public class UserCreateEndpointV1 : Endpoint<UserCreateRequest>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<UserCreateEndpointV1> _logger;
    private readonly RateLimitConfig _rateLimitConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserCreateEndpointV1"/> class with the specified command mediator, telemetry service, logger, and rate limit configuration.
    /// </summary>
    public UserCreateEndpointV1(
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<UserCreateEndpointV1> logger,
        IOptions<RateLimitConfig> rateLimitConfig)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
        _rateLimitConfig = rateLimitConfig.Value;
    }

    /// <summary>
    /// Configures the endpoint to handle HTTP POST requests at the route "/api/user/create" and allows anonymous access.
    /// Applies rate limiting based on configuration settings.
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/create");
        AllowAnonymous();

        // Rate Limiting 적용: 설정 파일에서 읽어온 값 사용
        if (_rateLimitConfig.UserCreateEndpoint.Enabled)
        {
            Throttle(
                hitLimit: _rateLimitConfig.UserCreateEndpoint.HitLimit,
                durationSeconds: _rateLimitConfig.UserCreateEndpoint.DurationSeconds,
                headerName: _rateLimitConfig.UserCreateEndpoint.HeaderName
            );
        }
    }

    /// <summary>
    /// Processes a user creation request, invoking the user creation command and returning an appropriate HTTP response based on the outcome.
    /// </summary>
    /// <param name="req">The user creation request containing name, email, and password.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    public override async Task HandleAsync(UserCreateRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("user.create");

        try
        {
            // 명령 실행
            var ret = await _commandMediator.SendAsync(
                new UserCreateCommand(req.Name, req.Email, req.Password),
                cancellationToken: ct);

            if (ret.Result.IsFailed)
            {
                // 실패 처리 - 구체적인 오류 메시지와 상태 코드 결정
                var errorMessage = ret.Result.GetErrorMessageAll();
                _logger.LogError("User creation command failed: {ErrorMessage}", errorMessage);
                
                var (statusCode, userFriendlyMessage) = GetErrorDetails(errorMessage);
                
                var errorResponse = new Demo.Application.Models.ErrorResponse
                {
                    Message = userFriendlyMessage,
                    ErrorCode = statusCode == 409 ? "DUPLICATE_EMAIL" : "VALIDATION_ERROR",
                    Details = errorMessage,
                    Timestamp = DateTime.UtcNow
                };

                await Send.ResponseAsync(errorResponse, statusCode, ct);
            }
            else
            {
                await Send.OkAsync(cancellation: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UserCreateEndpointV1));
            _telemetryService.SetActivityError(activity, ex);
            
            var errorResponse = new Demo.Application.Models.ErrorResponse
            {
                Message = "서버 내부 오류가 발생했습니다.",
                ErrorCode = "INTERNAL_ERROR",
                Details = ex.Message,
                Timestamp = DateTime.UtcNow
            };

            await Send.ResponseAsync(errorResponse, 500, ct);
        }
    }

    private static (int StatusCode, string UserFriendlyMessage) GetErrorDetails(string errorMessage)
    {
        // 데이터베이스 제약 조건 오류 분석
        if (errorMessage.Contains("duplicate key value violates unique constraint") && 
            errorMessage.Contains("users_email_key"))
        {
            return (409, "이미 사용 중인 이메일 주소입니다.");
        }
        
        if (errorMessage.Contains("duplicate key value violates unique constraint"))
        {
            return (409, "중복된 데이터입니다.");
        }
        
        if (errorMessage.Contains("violates not-null constraint"))
        {
            return (400, "필수 정보가 누락되었습니다.");
        }
        
        if (errorMessage.Contains("violates check constraint"))
        {
            return (400, "입력된 데이터가 유효하지 않습니다.");
        }

        // 기타 유효성 검사 오류
        if (errorMessage.Contains("validation", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            return (400, "입력된 정보를 확인해주세요.");
        }

        // 기본 오류
        return (500, "사용자 생성 중 오류가 발생했습니다.");
    }
}
