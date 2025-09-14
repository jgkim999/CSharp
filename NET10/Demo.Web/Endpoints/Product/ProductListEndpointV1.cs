using Demo.Application.Queries;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Queries.Abstractions;
using Demo.Application.Models;

namespace Demo.Web.Endpoints.Product;

public class ProductListEndpointV1 : Endpoint<ProductListRequest, ProductListResponse>
{
    private readonly IQueryMediator _queryMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ProductListEndpointV1> _logger;

    public ProductListEndpointV1(
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<ProductListEndpointV1> logger)
    {
        _queryMediator = queryMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/product/list");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ProductListRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("product.list");

        try
        {
            var query = new ProductListQuery(req.SearchTerm, req.CompanyId, req.Page, req.PageSize);
            var result = await _queryMediator.QueryAsync(query, ct);

            if (result.IsFailed)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                _logger.LogError("상품 목록 조회 실패: {ErrorMessage}", errorMessage);
                HttpContext.Response.StatusCode = 500;
                return;
            }

            Response = new ProductListResponse
            {
                Items = result.Value.Items,
                TotalItems = result.Value.TotalItems,
                Page = req.Page,
                PageSize = req.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "상품 목록 조회 중 예외 발생");
            _telemetryService.SetActivityError(activity, ex);
            HttpContext.Response.StatusCode = 500;
        }
    }
}