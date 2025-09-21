using Demo.Application.Queries;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;
using Demo.Application.Models;

namespace Demo.Web.Endpoints.Company;

/// <summary>
/// 회사 목록 조회를 위한 V1 엔드포인트 클래스
/// CQRS 패턴과 LiteBus 중재자를 사용하여 회사 목록 조회 쿼리를 처리합니다
/// OpenTelemetry 추적, 오류 처리 등을 지원합니다
/// </summary>
public class CompanyListEndpointV1 : Endpoint<CompanyListRequest, CompanyListResponse>
{
    private readonly IQueryMediator _queryMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<CompanyListEndpointV1> _logger;

    /// <summary>
    /// 회사 목록 조회 생성자
    /// </summary>
    /// <param name="queryMediator"></param>
    /// <param name="telemetryService"></param>
    /// <param name="logger"></param>
    public CompanyListEndpointV1(
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<CompanyListEndpointV1> logger)
    {
        _queryMediator = queryMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    /// <summary>
    /// 엔드포인트 설정
    /// </summary>
    public override void Configure()
    {
        Post("/api/company/list");
        AllowAnonymous();
    }

    /// <summary>
    /// 회사 목록 조회 요청 처리
    /// </summary>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    public override async Task HandleAsync(CompanyListRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("company.list");

        try
        {
            var query = new CompanyListQuery(req.SearchTerm, req.Page, req.PageSize);
            var result = await _queryMediator.QueryAsync(query, ct);

            if (result.IsFailed)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                _logger.LogError("회사 목록 조회 실패: {ErrorMessage}", errorMessage);
                HttpContext.Response.StatusCode = 500;
                return;
            }

            Response = new CompanyListResponse
            {
                Items = result.Value.Items,
                TotalItems = result.Value.TotalItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "회사 목록 조회 중 예외 발생");
            _telemetryService.SetActivityError(activity, ex);
            HttpContext.Response.StatusCode = 500;
        }
    }
}