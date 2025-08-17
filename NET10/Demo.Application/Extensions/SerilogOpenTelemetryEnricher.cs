using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Demo.Application.Extensions;

/// <summary>
/// OpenTelemetry 트레이스 정보를 Serilog 로그에 추가하는 Enricher
/// </summary>
public class OpenTelemetryEnricher : ILogEventEnricher
{
    /// <summary>
    /// 로그 이벤트에 OpenTelemetry 트레이스 정보를 추가합니다.
    /// </summary>
    /// <param name="logEvent">로그 이벤트</param>
    /// <param name="propertyFactory">속성 팩토리</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            // TraceId 추가
            if (activity.TraceId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString()));
            }

            // SpanId 추가
            if (activity.SpanId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString()));
            }

            // ParentId 추가 (선택적)
            if (activity.ParentSpanId != default)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ParentId", activity.ParentSpanId.ToString()));
            }

            // Operation Name 추가 (선택적)
            if (!string.IsNullOrEmpty(activity.OperationName))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("OperationName", activity.OperationName));
            }

            // Activity Tags 추가 (선택적)
            foreach (var tag in activity.Tags)
            {
                if (!string.IsNullOrEmpty(tag.Key) && !string.IsNullOrEmpty(tag.Value))
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty($"otel.{tag.Key}", tag.Value));
                }
            }
        }
        else
        {
            // Activity가 없는 경우 빈 값으로 설정
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", ""));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("SpanId", ""));
        }
    }
}

/// <summary>
/// OpenTelemetryEnricher를 위한 확장 메서드
/// </summary>
public static class OpenTelemetryEnricherExtensions
{
    /// <summary>
    /// LoggerConfiguration에 OpenTelemetry enricher를 추가합니다.
    /// </summary>
    /// <param name="enrichmentConfiguration">Enrichment 설정</param>
    /// <returns>LoggerConfiguration</returns>
    public static Serilog.LoggerConfiguration WithOpenTelemetry(
        this Serilog.Configuration.LoggerEnrichmentConfiguration enrichmentConfiguration)
    {
        return enrichmentConfiguration.With<OpenTelemetryEnricher>();
    }
}