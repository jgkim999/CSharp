using Demo.Application.Queries;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;
using Demo.Application.Models;

namespace Demo.Web.Endpoints.Company;

public class CompanyListEndpointV1 : Endpoint<CompanyListRequest, CompanyListResponse>
{
    private readonly IQueryMediator _queryMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<CompanyListEndpointV1> _logger;

    public CompanyListEndpointV1(
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<CompanyListEndpointV1> logger)
    {
        _queryMediator = queryMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/company/list");
        AllowAnonymous();
    }

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