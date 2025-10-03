using Demo.Application.Commands;
using Demo.Application.Configs;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using Demo.Application.Extensions;
using Demo.Application.Models;
using Microsoft.Extensions.Options;

namespace Demo.Web.Endpoints.Company;

public class CompanyCreateSummary : Summary<CompanyCreateEndpointV1>
{
    public CompanyCreateSummary()
    {
        Summary = "회사 생성";
        Description = "회사 생성 API입니다.";
        Response<EmptyResponse>(200, "성공");
    }
}

/// <summary>
/// 회사 생성 엔드포인트의 Swagger 문서화를 위한 요약 클래스
/// FastEndpoints의 Summary를 상속받아 API 문서를 정의합니다.
/// </summary>
public class CompanyCreateEndpointV1 : Endpoint<CompanyCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<CompanyCreateEndpointV1> _logger;
    private readonly RateLimitConfig _rateLimitConfig;

    /// <summary>
    /// 지정된 명령 중재자, 원격 측정 서비스, 로거 및 속도 제한 구성을 사용하여 CompanyCreateEndpointV1 클래스의 새 인스턴스를 초기화합니다.
    /// </summary>
    public CompanyCreateEndpointV1(
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<CompanyCreateEndpointV1> logger,
        IOptions<RateLimitConfig> rateLimitConfig)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
        _rateLimitConfig = rateLimitConfig.Value;
    }

    /// <summary>
    /// "/api/company/create" 경로에서 HTTP POST 요청을 처리하고 익명 액세스를 허용하도록 엔드포인트를 구성합니다.
    /// 구성 설정에 따라 속도 제한을 적용합니다.
    /// </summary>
    public override void Configure()
    {
        Post("/api/company/create");
        AllowAnonymous();
        Summary(new CompanyCreateSummary());
        //Version(1);
        Group<CompanyGroup>();

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
    /// 회사 생성 요청을 처리하여 회사 생성 명령을 호출하고 결과에 따라 적절한 HTTP 응답을 반환합니다.
    /// </summary>
    /// <param name="req">회사명이 포함된 회사 생성 요청.</param>
    /// <param name="ct">비동기 작업에 대한 취소 토큰.</param>
    public override async Task HandleAsync(CompanyCreateRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("company.create");

        try
        {
            var ret = await _commandMediator.SendAsync(
                new CompanyCreateCommand(req.Name),
                cancellationToken: ct);

            if (ret.Result.IsFailed)
            {
                var errorMessage = ret.Result.GetErrorMessageAll();
                _logger.LogError("회사 생성 명령 실패: {ErrorMessage}", errorMessage);
                AddError(errorMessage);
                await Send.ErrorsAsync(500, ct);
            }
            else
            {
                await Send.OkAsync(cancellation: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(CompanyCreateEndpointV1));
            AddError(ex.Message);
            _telemetryService.SetActivityError(activity, ex);
            await Send.ErrorsAsync(500, ct);
        }
    }
}