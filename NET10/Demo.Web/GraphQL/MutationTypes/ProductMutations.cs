using Demo.Application.Extensions;
using Demo.Application.Handlers.Commands;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Input;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Commands.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.MutationTypes;

public class ProductMutations
{
    /// <summary>
    /// CreateProductAsync 뮤테이션 정의
    /// 새로운 상품을 생성합니다
    /// </summary>
    /*
    mutation {
        createProduct(input: { companyId: 1, name: "노트북", price: 1500000 }) {
            success
            message
            errors
        }
    }
     */
    public async Task<CreateProductPayload> CreateProductAsync(
        CreateProductInput input,
        ICommandMediator commandMediator,
        ITelemetryService telemetryService,
        ILogger<ProductMutations> logger,
        CancellationToken cancellationToken)
    {
        using var activity = telemetryService.StartActivity("product.mutations.create.product");

        try
        {
            logger.LogInformation("상품 생성 시작: {Name}, 회사ID: {CompanyId}, 가격: {Price}",
                input.Name, input.CompanyId, input.Price);

            var result = await commandMediator.SendAsync(
                new ProductCreateCommand(input.CompanyId, input.Name, input.Price),
                cancellationToken: cancellationToken);

            if (result.Result.IsFailed)
            {
                var errorMessage = result.Result.GetErrorMessageAll();
                logger.LogError("상품 생성 명령 실패: {ErrorMessage}", errorMessage);
                return new CreateProductPayload(false, null, new List<string> { errorMessage });
            }

            logger.LogInformation("상품 생성 성공: {Name}", input.Name);
            return new CreateProductPayload(true, "상품이 성공적으로 생성되었습니다.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "상품 생성 중 예외 발생");
            telemetryService.SetActivityError(activity, ex);
            return new CreateProductPayload(false, null, new List<string> { ex.Message });
        }
    }
}
