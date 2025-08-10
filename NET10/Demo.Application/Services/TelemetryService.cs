using System.Diagnostics;
using System.Diagnostics.Metrics;
using Serilog.Context;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Demo.Application.Services;

/// <summary>
/// 사용자 정의 텔레메트리 서비스
/// </summary>
public sealed class TelemetryService : ITelemetryService, IDisposable
{
    /// <summary>
    /// Demo.Application 애플리케이션의 _activitySource
    /// </summary>
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Demo.Application 애플리케이션의 _meter
    /// </summary>
    private readonly Meter _meter;

    // 사용자 정의 메트릭 정의
    private readonly Counter<long> _requestCounter;
    private readonly Histogram<double> _requestDuration;
    private readonly Counter<long> _errorCounter;
    private readonly Gauge<int> _activeConnections;
    
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Counter<long>> _businessCounters
        = new();
    
    public string ActiveSourceName => _activitySource.Name;
    public string MeterName => _meter.Name;
    
    /// <summary>
    /// TelemetryService 생성자
    /// </summary>
    public TelemetryService(string serviceName, string serviceVersion)
    {
        _activitySource = new ActivitySource(serviceName, serviceVersion);
        _meter = new Meter(serviceName, serviceVersion);

        // 카운터 메트릭 초기화
        _requestCounter = _meter.CreateCounter<long>(
            name: "demo_app_requests_total",
            unit: "1",
            description: "Total number of requests processed");

        // 히스토그램 메트릭 초기화
        _requestDuration = _meter.CreateHistogram<double>(
            name: "demo_app_request_duration_seconds",
            unit: "s",
            description: "Duration of requests in seconds");

        // 에러 카운터 초기화
        _errorCounter = _meter.CreateCounter<long>(
            name: "demo_app_errors_total",
            unit: "1",
            description: "Total number of errors occurred");

        // 게이지 메트릭 초기화
        _activeConnections = _meter.CreateGauge<int>(
            name: "demo_app_active_connections",
            unit: "1",
            description: "Number of active connections");
    }

    /// <summary>
    /// 사용자 정의 Activity를 시작합니다.
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <param name="tags">추가할 태그</param>
    /// <returns>시작된 Activity</returns>
    public Activity? StartActivity(string operationName, Dictionary<string, object?>? tags = null)
    {
        var activity = _activitySource.StartActivity(operationName);

        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    /// <summary>
    /// HTTP 요청 메트릭을 기록합니다.
    /// </summary>
    /// <param name="method">HTTP 메서드</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="statusCode">상태 코드</param>
    /// <param name="duration">요청 처리 시간</param>
    public void RecordHttpRequest(string method, string endpoint, int statusCode, double duration)
    {
        var tags = new TagList
        {
            { "method", method },
            { "endpoint", endpoint },
            { "status_code", statusCode.ToString() }
        };

        _requestCounter.Add(1, tags);
        _requestDuration.Record(duration, tags);
    }

    /// <summary>
    /// 에러 메트릭을 기록합니다.
    /// </summary>
    /// <param name="errorType">에러 타입</param>
    /// <param name="operation">작업 이름</param>
    /// <param name="message">에러 메시지</param>
    public void RecordError(string errorType, string operation, string? message = null)
    {
        var tags = new TagList
        {
            { "error_type", errorType },
            { "operation", operation }
        };

        if (!string.IsNullOrEmpty(message))
        {
            tags.Add("message", message);
        }

        _errorCounter.Add(1, tags);
    }

    /// <summary>
    /// 비즈니스 메트릭을 기록합니다.
    /// </summary>
    /// <param name="metricName">메트릭 이름</param>
    /// <param name="value">값</param>
    /// <summary>
    /// Records a business-specific metric by incrementing a named counter with optional tags.
    /// </summary>
    /// <param name="metricName">The name of the business metric to record.</param>
    /// <param name="value">The value to add to the metric counter.</param>
    /// <param name="tags">Optional tags to associate with the metric data.</param>
    public void RecordBusinessMetric(string metricName, long value, Dictionary<string, object?>? tags = null)
    {
        var counter = _businessCounters.GetOrAdd(metricName, m =>
            _meter.CreateCounter<long>(
            name: $"business_{m}",
            unit: "1",
            description: $"Business metric: {m}"));

        var tagList = new TagList();
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                tagList.Add(tag.Key, tag.Value?.ToString());
            }
        }

        counter.Add(value, tagList);
    }

    /// <summary>
    /// Activity에 에러 정보를 설정합니다.
    /// </summary>
    /// <param name="activity">Activity 객체</param>
    /// <summary>
    /// Marks the specified activity as failed due to an exception and attaches detailed error information as tags and an event.
    /// </summary>
    /// <param name="activity">The activity to update with error status and exception details.</param>
    /// <param name="exception">The exception that caused the error.</param>
    public void SetActivityError(Activity? activity, Exception exception)
    {
        if (activity == null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("error", true);
        activity.SetTag("error.type", exception.GetType().Name);
        activity.SetTag("error.message", exception.Message);
        activity.SetTag("error.stack_trace", exception.StackTrace);

        // 예외를 Activity 이벤트로 기록
        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection
        {
            ["exception.type"] = exception.GetType().FullName,
            ["exception.message"] = exception.Message,
            ["exception.stacktrace"] = exception.StackTrace
        }));
    }

    /// <summary>
    /// Activity에 성공 상태를 설정합니다.
    /// </summary>
    /// <param name="activity">Activity 객체</param>
    /// <summary>
    /// Marks the specified activity as successful, setting its status to OK and adding a success tag.
    /// </summary>
    /// <param name="activity">The activity to update. If null, no action is taken.</param>
    /// <param name="message">An optional success message to associate with the activity status.</param>
    public void SetActivitySuccess(Activity? activity, string? message = null)
    {
        if (activity == null)
            return;

        activity.SetStatus(ActivityStatusCode.Ok, message ?? "Operation completed successfully");
        activity.SetTag("success", true);
    }

    /// <summary>
    /// 현재 트레이스 컨텍스트와 함께 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="level">로그 레벨</param>
    /// <param name="messageTemplate">메시지 템플릿</param>
    /// <summary>
    /// Logs a message at the specified level, attaching current activity trace context properties to the log entry if an activity is present.
    /// </summary>
    private void LogWithTraceContext(ILogger logger, LogLevel level, 
        string messageTemplate, params object[] propertyValues)
    {
        var activity = Activity.Current;
        if (activity == null)
            return;
        
        using (LogContext.PushProperty("TraceId", activity.TraceId.ToString()))
        using (LogContext.PushProperty("SpanId", activity.SpanId.ToString()))
        using (LogContext.PushProperty("ParentId", activity.ParentSpanId.ToString()))
        using (LogContext.PushProperty("OperationName", activity.OperationName))
        {
            logger.Log(level, messageTemplate, propertyValues);
        }
    }

    /// <summary>
    /// 현재 트레이스 컨텍스트와 함께 정보 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="messageTemplate">메시지 템플릿</param>
    /// <summary>
    /// Logs an informational message with the current activity's trace context, if available.
    /// </summary>
    /// <param name="logger">The logger to write the message to.</param>
    /// <param name="messageTemplate">The message template for the log entry.</param>
    /// <param name="propertyValues">Optional property values to include in the log entry.</param>
    public void LogInformationWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues)
    {
        LogWithTraceContext(logger, LogLevel.Information, messageTemplate, propertyValues);
    }

    /// <summary>
    /// 현재 트레이스 컨텍스트와 함께 경고 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="messageTemplate">메시지 템플릿</param>
    /// <summary>
    /// Logs a warning message with the current activity's trace context, if available.
    /// </summary>
    /// <param name="logger">The logger to write the warning message to.</param>
    /// <param name="messageTemplate">The message template for the warning log entry.</param>
    /// <param name="propertyValues">Optional property values to format the message template.</param>
    public void LogWarningWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues)
    {
        LogWithTraceContext(logger, LogLevel.Warning, messageTemplate, propertyValues);
    }

    /// <summary>
    /// 현재 트레이스 컨텍스트와 함께 에러 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="exception">예외 객체</param>
    /// <param name="messageTemplate">메시지 템플릿</param>
    /// <summary>
    /// Logs an error message with exception details, including trace context properties if an activity is present.
    /// </summary>
    /// <param name="logger">The logger to write the error message to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="messageTemplate">The message template for the log entry.</param>
    /// <param name="propertyValues">Values to format into the message template.</param>
    public void LogErrorWithTrace(ILogger logger, Exception exception, string messageTemplate, params object[] propertyValues)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            using (LogContext.PushProperty("TraceId", activity.TraceId.ToString()))
            using (LogContext.PushProperty("SpanId", activity.SpanId.ToString()))
            using (LogContext.PushProperty("ParentId", activity.ParentSpanId.ToString()))
            using (LogContext.PushProperty("OperationName", activity.OperationName))
            {
                logger.LogError(exception, messageTemplate, propertyValues);
            }
        }
        else
        {
            logger.LogError(exception, messageTemplate, propertyValues);
        }
    }

    /// <summary>
    /// 구조화된 로깅을 위한 로그 컨텍스트를 생성합니다.
    /// </summary>
    /// <param name="properties">추가할 속성들</param>
    /// <summary>
    /// Creates a structured logging context that includes current activity trace information and user-defined properties.
    /// </summary>
    /// <param name="properties">A dictionary of additional properties to include in the log context.</param>
    /// <returns>An <see cref="IDisposable"/> that, when disposed, removes the pushed log context properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="properties"/> is null.</exception>
    public IDisposable CreateLogContext(IReadOnlyDictionary<string, object> properties)
    {
        var disposables = new List<IDisposable>();
        if (properties is null)
            throw new ArgumentNullException(nameof(properties));
        
        // 현재 Activity의 트레이스 정보 추가
        var activity = Activity.Current;
        if (activity != null)
        {
            disposables.Add(LogContext.PushProperty("TraceId", activity.TraceId.ToString()));
            disposables.Add(LogContext.PushProperty("SpanId", activity.SpanId.ToString()));
            disposables.Add(LogContext.PushProperty("ParentId", activity.ParentSpanId.ToString()));
            disposables.Add(LogContext.PushProperty("OperationName", activity.OperationName));
        }

        // 사용자 정의 속성 추가
        foreach (var property in properties)
        {
            disposables.Add(LogContext.PushProperty(property.Key, property.Value));
        }

        return new CompositeDisposable(disposables);
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _activitySource?.Dispose();
        _meter?.Dispose();
    }
}

/// <summary>
/// 여러 IDisposable 객체를 관리하는 복합 Disposable 클래스
/// </summary>
public class CompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables;
    private bool _disposed = false;

    /// <summary>
    /// CompositeDisposable 생성자
    /// </summary>
    /// <param name="disposables">관리할 IDisposable 객체들</param>
    public CompositeDisposable(List<IDisposable> disposables)
    {
        _disposables = disposables ?? throw new ArgumentNullException(nameof(disposables));
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
