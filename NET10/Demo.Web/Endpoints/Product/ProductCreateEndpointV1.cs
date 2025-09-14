using Demo.Application.Commands;
using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using Demo.Application.Models;
using Demo.Application.Extensions;

namespace Demo.Web.Endpoints.Product;

public class ProductCreateEndpointV1 : Endpoint<ProductCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ProductCreateEndpointV1> _logger;

    public ProductCreateEndpointV1(
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<ProductCreateEndpointV1> logger)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/product/create");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ProductCreateRequest req, CancellationToken ct)
    {
        using var activity = _telemetryService.StartActivity("product.create");

        try
        {
            _logger.LogInformation("상품 생성 시작: {Name}, 회사ID: {CompanyId}, 가격: {Price}", req.Name, req.CompanyId, req.Price);

            var result = await _commandMediator.SendAsync(
                new ProductCreateCommand(req.CompanyId, req.Name, req.Price),
                cancellationToken: ct);

            if (result.Result.IsFailed)
            {
                var errorMessage = result.Result.GetErrorMessageAll();
                _logger.LogError("상품 생성 명령 실패: {ErrorMessage}", errorMessage);
                AddError(errorMessage);
                await Send.ErrorsAsync(500, ct);
            }
            else
            {
                _logger.LogInformation("상품 생성 성공: {Name}", req.Name);
                await Send.OkAsync(cancellation: ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(ProductCreateEndpointV1));
            AddError(ex.Message);
            _telemetryService.SetActivityError(activity, ex);
            await Send.ErrorsAsync(500, ct);
        }
    }
}