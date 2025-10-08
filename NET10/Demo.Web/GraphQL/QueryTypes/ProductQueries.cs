using Demo.Application.Handlers.Queries;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.QueryTypes;

public class ProductQueries
{
    /// <summary>
    /// GetProductListAsync 리졸버 정의
    /// 상품 목록을 조회하며, 회사별 필터링, 검색, 페이지네이션 기능을 지원합니다
    /// </summary>
    /// <param name="searchTerm">검색어</param>
    /// <param name="companyId">회사 ID (선택)</param>
    /// <param name="page">원하는 페이지 (0부터 시작)</param>
    /// <param name="pageSize">페이지 크기 (기본 10)</param>
    /// <param name="queryMediator">CQRS 쿼리 처리를 위한 IQueryMediator</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService</param>
    /// <param name="logger">로깅을 위한 ILogger</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>상품 목록과 페이지네이션 정보를 포함한 페이로드</returns>
    /*
    query {
        productList(searchTerm: "노트북", companyId: 1, page: 0, pageSize: 10) {
            items {
                id
                companyId
                companyName
                name
                price
                createdAt
            }
            totalItems
            page
            pageSize
            errors
        }
    }
     */
    public async Task<ProductListPayload> GetProductListAsync(
        string? searchTerm,
        long? companyId,
        int page,
        int pageSize,
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<ProductQueries> logger,
        CancellationToken cancellationToken)
    {
        using var activity = telemetryService.StartActivity("product.queries.get.product.list");

        try
        {
            var query = new ProductListQuery(searchTerm, companyId, page, pageSize);
            var result = await queryMediator.QueryAsync(query, cancellationToken);

            if (result.IsFailed)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                logger.LogError("상품 목록 조회 실패: {ErrorMessage}", errorMessage);
                var errors = result.Errors.Select(e => e.Message).ToList();
                return new ProductListPayload(null, 0, page, pageSize, errors);
            }

            return new ProductListPayload(result.Value.Items, result.Value.TotalItems, page, pageSize);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "상품 목록 조회 중 예외 발생");
            telemetryService.SetActivityError(activity, ex);
            return new ProductListPayload(null, 0, page, pageSize, new List<string> { ex.Message });
        }
    }
}
