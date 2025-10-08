using Demo.Application.Handlers.Queries;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;
using Demo.Application.Models;

namespace Demo.Web.Endpoints.User;

public class UserListSummary : Summary<UserListEndpointV1>
{
    public UserListSummary()
    {
        Summary = "사용자 목록 조회";
        Description = "사용자 목록을 조회합니다";
    }
}

/// <summary>
/// 사용자 목록 조회를 위한 V1 엔드포인트 클래스
/// CQRS 패턴과 LiteBus 중재자를 사용하여 사용자 목록 쿼리를 처리합니다
/// 페이지네이션과 검색 기능을 지원하며, OpenTelemetry 추적을 포함합니다
/// </summary>
public class UserListEndpointV1 : Endpoint<UserListRequest, UserListResponse>
{
    private readonly IQueryMediator _queryMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<UserListEndpointV1> _logger;

    /// <summary>
    /// UserListEndpointV1의 새 인스턴스를 초기화합니다
    /// CQRS 쿼리 중재자, 텔레메트리 서비스, 로거를 주입받습니다
    /// </summary>
    /// <param name="queryMediator">CQRS 쿼리 처리를 위한 IQueryMediator 인스턴스</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService 인스턴스</param>
    /// <param name="logger">로깅을 위한 ILogger 인스턴스</param>
    public UserListEndpointV1(
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<UserListEndpointV1> logger)
    {
        _queryMediator = queryMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    /// <summary>
    /// 엔드포인트의 라우팅 및 보안 설정을 구성합니다
    /// POST /api/user/list 경로로 익명 접근을 허용합니다
    /// </summary>
    public override void Configure()
    {
        Post("/api/user/list");
        AllowAnonymous();
        //Version(1);
        Group<UserGroup>();
        Summary(new UserListSummary());
    }

    /// <summary>
    /// 사용자 목록 조회 요청을 비동기적으로 처리합니다
    /// OpenTelemetry Activity를 생성하여 추적을 수행하고, CQRS 쿼리를 통해 사용자 목록을 조회합니다
    /// 페이지네이션과 검색 조건을 지원하며, 오류 상황에 대한 예외 처리를 포함합니다
    /// </summary>
    /// <param name="req">사용자 목록 조회 요청 데이터 (검색어, 페이지, 페이지 크기 포함)</param>
    /// <param name="ct">비동기 작업 취소를 위한 CancellationToken</param>
    /// <returns>사용자 목록 데이터와 전체 아이템 수를 포함한 응답</returns>
    public override async Task HandleAsync(UserListRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("user.list");

        try
        {
            var query = new UserListQuery(req.SearchTerm, req.Page, req.PageSize);
            var result = await _queryMediator.QueryAsync(query, ct);

            if (result.IsFailed)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                _logger.LogError("사용자 목록 조회 실패: {ErrorMessage}", errorMessage);
                HttpContext.Response.StatusCode = 500;
                return;
            }

            Response = new UserListResponse
            {
                Items = result.Value.Items,
                TotalItems = result.Value.TotalItems
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "사용자 목록 조회 중 예외 발생");
            _telemetryService.SetActivityError(activity, ex);
            HttpContext.Response.StatusCode = 500;
        }
    }
}
