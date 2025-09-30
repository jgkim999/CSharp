using System.Diagnostics;
using Demo.Application.Services;
using Demo.Web.GraphQL.Types.Payload;
using Microsoft.Extensions.Logging;

namespace Demo.Web.GraphQL.QueryTypes;

public class TestQueries
{
    /// <summary>
    /// GetTestLoggingAsync 리졸버 정의
    /// 로깅 및 OpenTelemetry 추적 기능을 테스트합니다
    /// </summary>
    /// <param name="telemetryService">OpenTelemetry 추적을 위한 ITelemetryService</param>
    /// <param name="logger">로깅을 위한 ILogger</param>
    /// <param name="cancellationToken">작업 취소 토큰</param>
    /// <returns>로깅 테스트 결과 페이로드</returns>
    /*
    query {
        testLogging {
            message
            traceId
            spanId
            timestamp
            errors
        }
    }
     */
    public async Task<TestLoggingPayload> GetTestLoggingAsync(
        ITelemetryService telemetryService,
        ILogger<TestQueries> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetryService.StartActivity("TestLogging", new Dictionary<string, object?>
            {
                ["test.type"] = "logging",
                ["test.timestamp"] = DateTimeOffset.UtcNow.ToString()
            });

            logger.LogInformation("테스트 로그 메시지 - 정보 레벨");
            logger.LogWarning("테스트 로그 메시지 - 경고 레벨");

            // TelemetryService의 헬퍼 메서드 사용
            telemetryService.LogInformationWithTrace(logger, "TelemetryService를 통한 로그 메시지: {TestValue}", "test-value-123");

            // 중첩된 Activity 테스트
            using var nestedActivity = telemetryService.StartActivity("NestedOperation", new Dictionary<string, object?>
            {
                ["nested.operation"] = "database_query",
                ["nested.table"] = "users"
            });

            logger.LogInformation("중첩된 Activity에서의 로그 메시지");

            // 에러 시뮬레이션
            try
            {
                throw new InvalidOperationException("테스트용 예외입니다");
            }
            catch (Exception ex)
            {
                telemetryService.LogErrorWithTrace(logger, ex, "예외 발생 테스트: {ErrorType}", ex.GetType().Name);
                telemetryService.SetActivityError(nestedActivity, ex);
            }

            telemetryService.SetActivitySuccess(activity, "로깅 테스트 완료");

            await Task.CompletedTask;

            return new TestLoggingPayload(
                "로깅 테스트가 완료되었습니다. 콘솔 로그를 확인해주세요.",
                Activity.Current?.TraceId.ToString(),
                Activity.Current?.SpanId.ToString(),
                DateTimeOffset.UtcNow.ToString("O")
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "테스트 로깅 중 예외 발생");
            return new TestLoggingPayload(null, null, null, null, new List<string> { ex.Message });
        }
    }
}
