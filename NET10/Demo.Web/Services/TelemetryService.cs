using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Demo.Web.Services;

/// <summary>
/// 사용자 정의 텔레메트리 서비스
/// </summary>
public class TelemetryService
{
  /// <summary>
  /// Demo.Web 애플리케이션의 ActivitySource
  /// </summary>
  public static readonly ActivitySource ActivitySource = new("Demo.Web");

  /// <summary>
  /// Demo.Web 애플리케이션의 Meter
  /// </summary>
  public static readonly Meter Meter = new("Demo.Web");

  // 사용자 정의 메트릭 정의
  private readonly Counter<long> _requestCounter;
  private readonly Histogram<double> _requestDuration;
  private readonly Counter<long> _errorCounter;
  private readonly Gauge<int> _activeConnections;

  /// <summary>
  /// TelemetryService 생성자
  /// </summary>
  public TelemetryService()
  {
    // 카운터 메트릭 초기화
    _requestCounter = Meter.CreateCounter<long>(
        name: "demo_web_requests_total",
        unit: "1",
        description: "Total number of HTTP requests processed");

    // 히스토그램 메트릭 초기화
    _requestDuration = Meter.CreateHistogram<double>(
        name: "demo_web_request_duration_seconds",
        unit: "s",
        description: "Duration of HTTP requests in seconds");

    // 에러 카운터 초기화
    _errorCounter = Meter.CreateCounter<long>(
        name: "demo_web_errors_total",
        unit: "1",
        description: "Total number of errors occurred");

    // 게이지 메트릭 초기화
    _activeConnections = Meter.CreateGauge<int>(
        name: "demo_web_active_connections",
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
    var activity = ActivitySource.StartActivity(operationName);

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
  /// <param name="tags">태그</param>
  public void RecordBusinessMetric(string metricName, long value, Dictionary<string, object?>? tags = null)
  {
    var counter = Meter.CreateCounter<long>(
        name: $"demo_web_{metricName}",
        unit: "1",
        description: $"Business metric: {metricName}");

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
  public static void SetActivityError(Activity? activity, Exception exception)
  {
    if (activity == null) return;

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
  public static void SetActivitySuccess(Activity? activity, string? message = null)
  {
    if (activity == null) return;

    activity.SetStatus(ActivityStatusCode.Ok, message ?? "Operation completed successfully");
    activity.SetTag("success", true);
  }

  /// <summary>
  /// 현재 활성 연결 수를 업데이트합니다.
  /// </summary>
  /// <param name="count">연결 수</param>
  public void UpdateActiveConnections(int count)
  {
    // 게이지 메트릭은 콜백을 통해 구현
    // 실제 구현에서는 별도의 상태 관리가 필요
  }

  /// <summary>
  /// 리소스 정리
  /// </summary>
  public void Dispose()
  {
    ActivitySource?.Dispose();
    Meter?.Dispose();
  }
}

/// <summary>
/// TelemetryService를 위한 확장 메서드
/// </summary>
public static class TelemetryServiceExtensions
{
  /// <summary>
  /// TelemetryService를 DI 컨테이너에 등록합니다.
  /// </summary>
  /// <param name="services">서비스 컬렉션</param>
  /// <returns>서비스 컬렉션</returns>
  public static IServiceCollection AddTelemetryService(this IServiceCollection services)
  {
    services.AddSingleton<TelemetryService>();
    return services;
  }
}