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
    /// <summary>
/// Starts a telemetry activity with the specified operation name and optional tags.
/// </summary>
/// <param name="operationName">The name of the operation to associate with the activity.</param>
/// <param name="tags">Optional contextual tags to attach to the activity.</param>
/// <returns>The started <see cref="Activity"/> instance, or null if the activity could not be started.</returns>
    Activity? StartActivity(string operationName, Dictionary<string, object?>? tags = null);

    /// <summary>
    /// HTTP 요청 메트릭을 기록합니다.
    /// </summary>
    /// <param name="method">HTTP 메서드</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="statusCode">상태 코드</param>
    /// <summary>
/// Records metrics for an HTTP request, including method, endpoint, status code, and duration.
/// </summary>
/// <param name="method">The HTTP method used for the request (e.g., GET, POST).</param>
/// <param name="endpoint">The endpoint or route accessed by the request.</param>
/// <param name="statusCode">The HTTP status code returned by the request.</param>
/// <param name="duration">The time taken to process the request, in milliseconds.</param>
    void RecordHttpRequest(string method, string endpoint, int statusCode, double duration);

    /// <summary>
    /// 에러 메트릭을 기록합니다.
    /// </summary>
    /// <param name="errorType">에러 타입</param>
    /// <param name="operation">작업 이름</param>
    /// <summary>
/// Records an error metric with the specified error type and operation name, optionally including a descriptive message.
/// </summary>
/// <param name="errorType">The category or type of the error being recorded.</param>
/// <param name="operation">The name of the operation during which the error occurred.</param>
/// <param name="message">An optional message describing the error.</param>
    void RecordError(string errorType, string operation, string? message = null);

    /// <summary>
    /// 비즈니스 메트릭을 기록합니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름</param>
    /// <param name="value">값</param>
    /// <summary>
/// Records a business-related metric with the specified name, value, and optional contextual tags.
/// </summary>
/// <param name="metricName">The name of the business metric to record.</param>
/// <param name="value">The value associated with the metric.</param>
/// <param name="tags">Optional key-value pairs providing additional context for the metric.</param>
    void RecordBusinessMetric(string metricName, long value, Dictionary<string, object?>? tags = null);

    /// <summary>
    /// Marks the specified telemetry activity as successful, optionally including a message.
    /// </summary>
    /// <param name="activity">The activity to mark as successful.</param>
    /// <summary>
/// Marks the specified telemetry activity as successful, optionally including a descriptive message.
/// </summary>
/// <param name="activity">The activity to mark as successful.</param>
/// <param name="message">An optional message describing the success.</param>
    void SetActivitySuccess(Activity? activity, string? message = null);
    
    /// <summary>
    /// Marks the specified activity as failed due to the provided exception.
    /// <summary>
/// Marks the specified telemetry activity as failed due to the provided exception.
/// </summary>
/// <param name="activity">The activity to mark as failed. If null, no action is taken.</param>
/// <param name="exception">The exception that caused the activity to fail.</param>
    void SetActivityError(Activity? activity, Exception exception);

    /// <summary>
    /// Logs an informational message with trace context using the provided logger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="messageTemplate">The message template for the log entry.</param>
    /// <summary>
/// Logs an informational message with trace context using the specified message template and optional property values.
/// </summary>
/// <param name="messageTemplate">The message template to log.</param>
/// <param name="propertyValues">Optional property values to format the message template.</param>
    void LogInformationWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// Logs a warning message with trace context using the provided logger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="messageTemplate">The message template for the warning log entry.</param>
    /// <summary>
/// Logs a warning message with trace context using the specified logger and message template.
/// </summary>
/// <param name="messageTemplate">The message template to log.</param>
/// <param name="propertyValues">Optional property values to format the message template.</param>
    void LogWarningWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// Logs an error message with trace context and exception details using the specified logger.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="exception">The exception to include in the log entry.</param>
    /// <param name="messageTemplate">The message template for the log entry.</param>
    /// <summary>
/// Logs an error message with trace context and exception details using the specified logger.
/// </summary>
/// <param name="exception">The exception to include in the log entry.</param>
/// <param name="messageTemplate">The message template for the log entry.</param>
/// <param name="propertyValues">Optional property values to format the message template.</param>
    void LogErrorWithTrace(ILogger logger, Exception exception, string messageTemplate, params object[] propertyValues);
}
