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
    /// <returns>The started <see cref="Activity"/> instance, or null if the activity could not be started.</returns>
    Activity? StartActivity(string operationName, Dictionary<string, object?>? tags = null);
    Activity? StartActivity(string operationName, ActivityKind kind, Dictionary<string, object?>? tags = null);
    Activity? StartActivity(string operationName, ActivityKind kind, ActivityContext? parentContext,
        Dictionary<string, object?>? tags = null);
    
    /// <summary>
    /// HTTP 요청 메트릭을 기록합니다.
    /// </summary>
    /// <param name="method">HTTP 메서드</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="statusCode">상태 코드</param>
    /// <param name="duration">The time taken to process the request, in milliseconds.</param>
    void RecordHttpRequest(string method, string endpoint, int statusCode, double duration);

    /// <summary>
    /// 에러 메트릭을 기록합니다.
    /// </summary>
    /// <param name="errorType">에러 타입</param>
    /// <param name="operation">작업 이름</param>
    /// <param name="message">An optional message describing the error.</param>
    void RecordError(string errorType, string operation, string? message = null);

    /// <summary>
    /// 비즈니스 메트릭을 기록합니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름</param>
    /// <param name="value">값</param>
    /// <param name="tags">Optional key-value pairs providing additional context for the metric.</param>
    void RecordBusinessMetric(string metricName, long value, Dictionary<string, object?>? tags = null);

    /// <summary>
    /// Marks the specified telemetry activity as successful, optionally including a message.
    /// </summary>
    /// <param name="activity">The activity to mark as successful.</param>
    /// <param name="message">An optional message describing the success.</param>
    void SetActivitySuccess(Activity? activity, string? message = null);
    
    /// <summary>
    /// Marks the specified activity as failed due to the provided exception.
    /// </summary>
    void SetActivityError(Activity? activity, Exception exception);

    /// <summary>
    /// Logs an informational message with trace context using the provided logger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="messageTemplate">The message template for the log entry.</param>
    /// <param name="propertyValues">Optional property values to format the message template.</param>
    void LogInformationWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// Logs a warning message with trace context using the provided logger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="messageTemplate">The message template for the warning log entry.</param>
    /// <param name="propertyValues">Optional property values to format the message template.</param>
    void LogWarningWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// Logs an error message with trace context and exception details using the specified logger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception">The exception to include in the log entry.</param>
    /// <param name="messageTemplate">The message template for the log entry.</param>
    /// <param name="propertyValues">Optional property values to format the message template.</param>
    void LogErrorWithTrace(ILogger logger, Exception exception, string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// RTT(Round Trip Time) 메트릭을 기록합니다.
    /// 이 메서드는 RTT 카운터, 히스토그램, 네트워크 품질, 게이지 메트릭을 기록합니다.
    /// </summary>
    /// <param name="countryCode">국가 코드 (null 또는 빈 문자열일 수 없음)</param>
    /// <param name="rtt">RTT 값 (초 단위, 음수일 수 없음)</param>
    /// <param name="quality">네트워크 품질 점수 (0-100 범위의 유효한 값이어야 함)</param>
    /// <param name="gameType">게임 타입 (기본값: "sod")</param>
    /// <exception cref="ArgumentException">countryCode가 null 또는 빈 문자열인 경우</exception>
    /// <exception cref="ArgumentOutOfRangeException">rtt가 음수이거나 quality가 유효 범위를 벗어난 경우</exception>
    void RecordRttMetrics(string countryCode, double rtt, double quality, string gameType = "sod");
}
