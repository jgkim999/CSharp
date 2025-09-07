using Demo.Application.Queries;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;
using Demo.Application.Models;

namespace Demo.Web.Endpoints.User;

public class UserListEndpointV1 : Endpoint<UserListRequest, UserListResponse>
{
    private readonly IQueryMediator _queryMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<UserListEndpointV1> _logger;

    public UserListEndpointV1(
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<UserListEndpointV1> logger)
    {
        _queryMediator = queryMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/user/list");
        AllowAnonymous();
    }

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
