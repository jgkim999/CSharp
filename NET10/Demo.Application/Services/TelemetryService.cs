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
    private readonly ILogger<TelemetryService> _logger;
    
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
    
    // RTT 관련 메트릭 정의
    private readonly Counter<long> _rttCounter;
    private readonly Histogram<double> _rttHistogram;
    private readonly Histogram<double> _networkQualityHistogram;
    private readonly Gauge<double> _rttGauge;
    
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, Counter<long>> _businessCounters
        = new();

    string ITelemetryService.ActiveSourceName => _activitySource.Name;
    string ITelemetryService.MeterName => _meter.Name;
    
    /// <summary>
    /// TelemetryService 생성자
    /// </summary>
    public TelemetryService(string serviceName, string serviceVersion, ILogger<TelemetryService> logger)
    {
        _logger = logger;
        _activitySource = new ActivitySource(serviceName, serviceVersion);
        _meter = new Meter(serviceName, serviceVersion);

        // 카운터 메트릭 초기화
        _requestCounter = _meter.CreateCounter<long>(
            name: "requests_total",
            unit: "1",
            description: "Total number of requests processed");

        // 히스토그램 메트릭 초기화
        _requestDuration = _meter.CreateHistogram<double>(
            name: "request_duration_seconds",
            unit: "s",
            description: "Duration of requests in seconds");

        // 에러 카운터 초기화
        _errorCounter = _meter.CreateCounter<long>(
            name: "errors_total",
            unit: "1",
            description: "Total number of errors occurred");

        // 게이지 메트릭 초기화
        _activeConnections = _meter.CreateGauge<int>(
            name: "active_connections",
            unit: "1",
            description: "Number of active connections");

        // RTT 관련 메트릭 초기화
        _rttCounter = _meter.CreateCounter<long>(
            name: "rtt_calls_total",
            unit: "1",
            description: "Total number of RTT measurements recorded");

        _rttHistogram = _meter.CreateHistogram<double>(
            name: "rtt_duration_seconds",
            unit: "s",
            description: "Distribution of RTT measurements in seconds");

        _networkQualityHistogram = _meter.CreateHistogram<double>(
            name: "network_quality_score",
            unit: "1",
            description: "Distribution of network quality scores");

        _rttGauge = _meter.CreateGauge<double>(
            name: "rtt_current_seconds",
            unit: "s",
            description: "Current RTT measurement in seconds");
    }

    /// <summary>
    /// 사용자 정의 Activity를 시작합니다.
    /// </summary>
    /// <param name="operationName">작업 이름</param>
    /// <param name="tags">추가할 태그</param>
    /// <returns>시작된 Activity</returns>
    public Activity? StartActivity(string operationName, Dictionary<string, object?>? tags)
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

    public Activity? StartActivity(string operationName, ActivityKind kind, Dictionary<string, object?>? tags = null)
    {
        var activity = _activitySource.StartActivity(operationName, kind);

        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    public Activity? StartActivity(string operationName, ActivityKind kind, ActivityContext? parentContext, Dictionary<string, object?>? tags)
    {
        if (parentContext is null)
            return null;
        var span = _activitySource.StartActivity(operationName, kind, parentContext.Value);
        if (span is null)
            return null;
        if (tags != null)
        {
            foreach (var tag in tags)
            {
                span.SetTag(tag.Key, tag.Value);
            }
        }
        return span;
    }

    public Activity? StartActivity(string operationName, ActivityKind kind, string? parentTraceId)
    {
        if (parentTraceId is null)
            return null;
        var span = _activitySource.StartActivity(operationName, kind, parentTraceId);
        return span;
    }

    /// <summary>
    /// HTTP 요청 메트릭을 기록합니다.
    /// </summary>
    /// <param name="method">HTTP 메서드</param>
    /// <param name="endpoint">엔드포인트</param>
    /// <param name="statusCode">상태 코드</param>
    /// <param name="duration">요청 처리 시간</param>
    void ITelemetryService.RecordHttpRequest(string method, string endpoint, int statusCode, double duration)
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
    void ITelemetryService.RecordError(string errorType, string operation, string? message)
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
    /// <param name="tags">태그</param>
    void ITelemetryService.RecordBusinessMetric(string metricName, long value, Dictionary<string, object?>? tags)
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
    /// <param name="exception">예외 객체</param>
    void ITelemetryService.SetActivityError(Activity? activity, Exception exception)
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
    /// <param name="message">성공 메시지</param>
    void ITelemetryService.SetActivitySuccess(Activity? activity, string? message)
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
    /// <param name="propertyValues">속성 값들</param>
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
    /// <param name="propertyValues">속성 값들</param>
    void ITelemetryService.LogInformationWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues)
    {
        LogWithTraceContext(logger, LogLevel.Information, messageTemplate, propertyValues);
    }

    /// <summary>
    /// 현재 트레이스 컨텍스트와 함께 경고 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="messageTemplate">메시지 템플릿</param>
    /// <param name="propertyValues">속성 값들</param>
    void ITelemetryService.LogWarningWithTrace(ILogger logger, string messageTemplate, params object[] propertyValues)
    {
        LogWithTraceContext(logger, LogLevel.Warning, messageTemplate, propertyValues);
    }

    /// <summary>
    /// 활성 연결 수를 업데이트합니다.
    /// </summary>
    /// <param name="connectionCount">연결 수</param>
    public void UpdateActiveConnections(int connectionCount)
    {
        _activeConnections.Record(connectionCount);
    }

    /// <summary>
    /// 현재 트레이스 컨텍스트와 함께 에러 로그를 기록합니다.
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="exception">예외 객체</param>
    /// <param name="messageTemplate">메시지 템플릿</param>
    /// <param name="propertyValues">속성 값들</param>
    void ITelemetryService.LogErrorWithTrace(ILogger logger, Exception exception, string messageTemplate, params object[] propertyValues)
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
    /// RTT(Round Trip Time) 메트릭을 기록합니다.
    /// 이 메서드는 RTT 카운터, 히스토그램, 네트워크 품질, 게이지 메트릭을 기록합니다.
    /// </summary>
    /// <param name="countryCode">국가 코드 (null 또는 빈 문자열일 수 없음)</param>
    /// <param name="rtt">RTT 값 (초 단위, 음수일 수 없음)</param>
    /// <param name="quality">네트워크 품질 점수 (0-100 범위의 유효한 값이어야 함)</param>
    /// <param name="gameType">게임 타입 (기본값: "sod")</param>
    /// <exception cref="ArgumentException">countryCode가 null 또는 빈 문자열인 경우</exception>
    /// <exception cref="ArgumentOutOfRangeException">rtt가 음수이거나 quality가 유효 범위를 벗어난 경우</exception>
    void ITelemetryService.RecordRttMetrics(string countryCode, double rtt, double quality, string gameType)
    {
        try
        {
            // 입력 매개변수 유효성 검사
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                throw new ArgumentException("국가 코드는 null 또는 빈 문자열일 수 없습니다.", nameof(countryCode));
            }

            if (rtt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rtt), rtt, "RTT 값은 음수일 수 없습니다.");
            }

            if (quality < 0 || quality > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(quality), quality, "네트워크 품질 점수는 0-100 범위 내의 값이어야 합니다.");
            }

            if (string.IsNullOrWhiteSpace(gameType))
            {
                gameType = "sod"; // 기본값 설정
            }

            // TagList를 사용한 메트릭 태그 생성
            var rttTags = new TagList
            {
                { "country", countryCode },
                { "game", gameType }
            };

            // 각 메트릭 타입별 기록 로직 구현
            
            // RTT 카운터 증가 (호출 횟수 기록)
            _rttCounter.Add(1, rttTags);

            // RTT 히스토그램 기록 (RTT 분포 추적)
            _rttHistogram.Record(rtt, rttTags);

            // 네트워크 품질 히스토그램 기록 (품질 점수 분포 추적)
            _networkQualityHistogram.Record(quality, rttTags);

            // RTT 게이지 기록 (현재 RTT 값)
            _rttGauge.Record(rtt, rttTags);
        }
        catch (ArgumentException ex)
        {
            // 매개변수 유효성 검사 예외는 다시 던짐
            _logger.LogError(ex, nameof(TelemetryService));
            throw;
        }
        catch (Exception ex)
        {
            // 예상치 못한 예외 발생 시 로깅 후 무시 (메트릭 기록 실패가 애플리케이션 흐름에 영향을 주지 않도록)
            // 로거가 없으므로 System.Diagnostics.Debug를 사용하여 디버그 출력
            _logger.LogDebug(ex, "RTT 메트릭 기록 중 예외 발생: {Message}", ex.Message);
            
            // 메트릭 기록 실패는 애플리케이션 흐름을 중단시키지 않음
            // 따라서 예외를 다시 던지지 않음
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Dispose()
    {
        _activitySource.Dispose();
        _meter.Dispose();
    }
}
