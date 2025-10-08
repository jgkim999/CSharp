using Demo.Application.Services;
using FastEndpoints;
using LiteBus.Commands.Abstractions;
using Demo.Application.Models;
using Demo.Application.Extensions;
using Demo.Application.Handlers.Commands;

namespace Demo.Web.Endpoints.Product;

public class ProductCreateSummary : Summary<ProductCreateEndpointV1>
{
    public ProductCreateSummary()
    {
        Summary = "상품 생성";
        Description = "상품을 생성합니다";
        Response(200, "성공");
        Response(500, "서버 에러");
    }
}

/// <summary>
/// 상품 생성을 위한 V1 엔드포인트 클래스
/// CQRS 패턴과 LiteBus 중재자를 사용하여 상품 생성 커맨드를 처리합니다
/// OpenTelemetry 추적과 오류 처리를 지원합니다
/// </summary>
public class ProductCreateEndpointV1 : Endpoint<ProductCreateRequest, EmptyResponse>
{
    private readonly ICommandMediator _commandMediator;
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<ProductCreateEndpointV1> _logger;

    /// <summary>
    /// ProductCreateEndpointV1의 새 인스턴스를 초기화합니다
    /// CQRS 커맨드 중재자, 텔레메트리 서비스, 로거를 주입받습니다
    /// </summary>
    /// <param name="commandMediator">CQRS 커맨드 처리를 위한 ICommandMediator 인스턴스</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService 인스턴스</param>
    /// <param name="logger">로깅을 위한 ILogger 인스턴스</param>
    public ProductCreateEndpointV1(
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<ProductCreateEndpointV1> logger)
    {
        _commandMediator = commandMediator;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    /// <summary>
    /// 엔드포인트의 라우팅 및 보안 설정을 구성합니다
    /// POST /api/product/create 경로로 익명 접근을 허용합니다
    /// </summary>
    public override void Configure()
    {
        Post("/api/product/create");
        AllowAnonymous();
        Group<ProductGroup>();
        //Version(1);
        Summary(new ProductCreateSummary());
    }

    /// <summary>
    /// 상품 생성 요청을 비동기적으로 처리합니다
    /// OpenTelemetry Activity를 생성하여 추적을 수행하고, CQRS 커맨드를 통해 상품을 생성합니다
    /// 상품 생성 과정에서 발생하는 오류를 처리하고 적절한 HTTP 응답을 반환합니다
    /// </summary>
    /// <param name="req">상품 생성 요청 데이터 (회사ID, 상품명, 가격 포함)</param>
    /// <param name="ct">비동기 작업 취소를 위한 CancellationToken</param>
    /// <returns>상품 생성 결과에 따른 HTTP 응답</returns>
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