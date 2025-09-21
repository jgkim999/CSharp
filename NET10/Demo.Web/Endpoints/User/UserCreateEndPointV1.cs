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
/// 사용자 생성 엔드포인트의 Swagger 문서화를 위한 요약 클래스
/// FastEndpoints의 Summary를 상속받아 API 문서를 정의합니다
/// </summary>
public class UserCreateEndpointSummary : Summary<UserCreateEndpointV1>
{
    /// <summary>
    /// UserCreateEndpointSummary의 새 인스턴스를 초기화하고 Swagger 문서 정보를 설정합니다
    /// CQRS 패턴을 사용하여 사용자 생성 커맨드를 처리하며, Rate Limiting을 지원합니다
    /// </summary>
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

/// <summary>
/// 사용자 생성을 위한 V1 엔드포인트 클래스
/// CQRS 패턴과 LiteBus 중재자를 사용하여 사용자 생성 커맨드를 처리합니다
/// Rate Limiting, OpenTelemetry 추적, 오류 처리 등을 지원합니다
/// </summary>
public class UserCreateEndpointV1 : Endpoint<UserCreateRequest>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<UserCreateEndpointV1> _logger;
    private readonly RateLimitConfig _rateLimitConfig;

    /// <summary>
    /// UserCreateEndpointV1의 새 인스턴스를 초기화합니다
    /// CQRS 커맨드 중재자, 텔레메트리 서비스, 로거, Rate Limit 설정을 주입받습니다
    /// </summary>
    /// <param name="commandMediator">CQRS 커맨드 처리를 위한 ICommandMediator 인스턴스</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService 인스턴스</param>
    /// <param name="logger">로깅을 위한 ILogger 인스턴스</param>
    /// <param name="rateLimitConfig">Rate Limiting 설정을 위한 RateLimitConfig</param>
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
    /// 엔드포인트의 라우팅, 보안, Rate Limiting 설정을 구성합니다
    /// POST /api/user/create 경로로 익명 접근을 허용하며, 설정에 따라 Rate Limiting을 적용합니다
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
    /// 사용자 생성 요청을 비동기적으로 처리합니다
    /// OpenTelemetry Activity를 생성하여 추적을 수행하고, CQRS 커맨드를 통해 사용자를 생성합니다
    /// 오류 상황에 따라 적절한 HTTP 상태 코드와 사용자 친화적 메시지를 반환합니다
    /// </summary>
    /// <param name="req">사용자 생성 요청 데이터 (이름, 이메일, 비밀번호 포함)</param>
    /// <param name="ct">비동기 작업 취소를 위한 CancellationToken</param>
    /// <returns>사용자 생성 결과에 따른 HTTP 응답</returns>
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

    /// <summary>
    /// 오류 메시지를 분석하여 적절한 HTTP 상태 코드와 사용자 친화적 메시지를 반환합니다
    /// 데이터베이스 제약 조건 오류, 유효성 검사 오류 등을 분류하여 처리합니다
    /// </summary>
    /// <param name="errorMessage">분석할 오류 메시지</param>
    /// <returns>HTTP 상태 코드와 사용자 친화적 메시지를 포함한 튜플</returns>
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
