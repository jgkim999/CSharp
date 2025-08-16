using Demo.Application.Configs;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Demo.Application.Extensions;

/// <summary>
/// OpenTelemetry Application 레이어 설정을 위한 확장 메서드
/// </summary>
public static class OpenTelemetryApplicationExtensions
{
    /// <summary>
    /// OpenTelemetry의 기본 설정 및 Application 레이어 관련 서비스를 구성합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="config">OpenTelemetry 구성 설정</param>
    /// <returns>구성된 OpenTelemetryBuilder</returns>
    public static OpenTelemetryBuilder AddOpenTelemetryApplication(this IServiceCollection services, OtelConfig config)
    {
        var openTelemetryBuilder = services.AddOpenTelemetry();
        
        // 샘플링 확률 설정
        if (!double.TryParse(config.TracesSamplerArg, out var probability))
        {
            probability = 1.0;
        }

        // 전체 OpenTelemetry 리소스 설정
        openTelemetryBuilder.ConfigureResource(resource => resource
            .AddService(serviceName: config.ServiceName, serviceVersion: config.ServiceVersion));

        // 추적 설정
        openTelemetryBuilder.WithTracing(tracing =>
        {
            tracing.AddSource(config.ServiceName);
            tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
        });

        // 메트릭 설정
        openTelemetryBuilder.WithMetrics(metrics =>
        {
            // TelemetryService의 MeterName을 OpenTelemetry에 등록
            metrics.AddMeter(config.ServiceName);
        });

        // Tracer 서비스 등록
        openTelemetryBuilder.Services.AddSingleton(TracerProvider.Default.GetTracer(config.ServiceName));

        return openTelemetryBuilder;
    }
}