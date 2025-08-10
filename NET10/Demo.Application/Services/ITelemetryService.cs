using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Demo.Application.Services;

public interface ITelemetryService
{
    string ActiveSourceName { get; }
    string MeterName { get; }

    /// <summary>
    /// 사용자 정의 Activity를 시작합니다.
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <param name="tags">추가할 태그</param>
    /// <returns>시작된 Activity</returns>
    Activity? StartActivity(string operationName, Dictionary<string, object?>? tags = null);

    /// <summary>
    /// HTTP 요청 메트릭을 기록합니다.
    /// </summary>
    /// <param name="method">HTTP 메서드</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="statusCode">상태 코드</param>
    /// <param name="duration">요청 처리 시간</param>
    void RecordHttpRequest(string method, string endpoint, int statusCode, double duration);

    /// <summary>
    /// 에러 메트릭을 기록합니다.
    /// </summary>
    /// <param name="errorType">에러 타입</param>
    /// <param name="operation">작업 이름</param>
    /// <param name="message">에러 메시지</param>
    void RecordError(string errorType, string operation, string? message = null);

    /// <summary>
    /// 비즈니스 메트릭을 기록합니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름</param>
    /// <param name="value">값</param>
    /// <param name="tags">태그</param>
    void RecordBusinessMetric(string metricName, long value, Dictionary<string, object?>? tags = null);

    void SetActivitySuccess(Activity? activity, string? message = null);
    void SetActivityError(Activity? activity, Exception exception);
    void LogInformationWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues);
    void LogWarningWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues);
    void LogErrorWithTrace(ILogger logger, Exception exception, string messageTemplate, params object[] propertyValues);
}
