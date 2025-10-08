using Demo.Application.Handlers.Queries;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.QueryTypes;

public class CompanyQueries
{
    /// <summary>
    /// GetCompanyListAsync 리졸버 정의
    /// 회사 목록을 조회하며, 검색과 페이지네이션 기능을 지원합니다
    /// </summary>
    /// <param name="searchTerm">검색어</param>
    /// <param name="page">원하는 페이지 (0부터 시작)</param>
    /// <param name="pageSize">페이지 크기 (기본 10)</param>
    /// <param name="queryMediator">CQRS 쿼리 처리를 위한 IQueryMediator</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService</param>
    /// <param name="logger">로깅을 위한 ILogger</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>회사 목록과 전체 개수를 포함한 페이로드</returns>
    /*
    query {
        companyList(searchTerm: "삼성", page: 0, pageSize: 10) {
            items {
                id
                name
                createdAt
            }
            totalItems
            errors
        }
    }
     */
    public async Task<CompanyListPayload> GetCompanyListAsync(
        string? searchTerm,
        int page,
        int pageSize,
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<CompanyQueries> logger,
        CancellationToken cancellationToken)
    {
        using var activity = telemetryService.StartActivity("company.queries.get.company.list");

        try
        {
            var query = new CompanyListQuery(searchTerm, page, pageSize);
            var result = await queryMediator.QueryAsync(query, cancellationToken);

            if (result.IsFailed)
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                logger.LogError("회사 목록 조회 실패: {ErrorMessage}", errorMessage);
                var errors = result.Errors.Select(e => e.Message).ToList();
                return new CompanyListPayload(null, 0, errors);
            }

            return new CompanyListPayload(result.Value.Items, result.Value.TotalItems);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "회사 목록 조회 중 예외 발생");
            telemetryService.SetActivityError(activity, ex);
            return new CompanyListPayload(null, 0, new List<string> { ex.Message });
        }
    }
}
