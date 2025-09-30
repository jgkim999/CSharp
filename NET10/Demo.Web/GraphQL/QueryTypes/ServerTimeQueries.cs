using System.Diagnostics;
using Demo.Application.Queries;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Payload;
using LiteBus.Queries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.QueryTypes;

public class ServerTimeQueries
{
    /// <summary>
    /// GetServerTimeAsync 리졸버 정의
    /// 서버 시간을 UTC, 한국 시간, 음력으로 반환합니다
    /// </summary>
    /// <param name="queryMediator">CQRS 쿼리 처리를 위한 IQueryMediator</param>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService</param>
    /// <param name="logger">로깅을 위한 ILogger</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>서버 시간 정보를 포함한 페이로드</returns>
    /*
    query {
        serverTime {
            utc
            korea
            koreanCalendar
            errors
        }
    }
     */
    public async Task<ServerTimePayload> GetServerTimeAsync(
        IQueryMediator queryMediator,
        ITelemetryService telemetryService,
        ILogger<ServerTimeQueries> logger,
        CancellationToken cancellationToken)
    {
        Activity? parentActivity = Activity.Current;
        using Activity? span = telemetryService.StartActivity("server.time.queries.get.server.time", ActivityKind.Internal, parentActivity?.Context);

        try
        {
            var query = new ServerTimeQuery();
            var result = await queryMediator.QueryAsync(query, cancellationToken);

            return new ServerTimePayload(result.utc, result.korea, result.koreanCalendar);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "서버 시간 조회 중 예외 발생");
            telemetryService.SetActivityError(span, ex);
            return new ServerTimePayload(null, null, null, new List<string> { ex.Message });
        }
    }
}
