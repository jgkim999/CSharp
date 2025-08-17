using Demo.Application.Configs;
using Demo.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Demo.Application.Extensions;

/// <summary>
/// OpenTelemetry Application 레이어 설정을 위한 확장 메서드
/// </summary>
public static class OpenTelemetryApplicationExtensions
{
    /// <summary>
    /// OpenTelemetry의 기본 설정 및 Application 레이어 관련 서비스를 구성합니다
    /// </summary>
    /// <param name="builder"></param>
    /// <returns>구성된 OpenTelemetryBuilder</returns>
    public static (WebApplicationBuilder builder, OpenTelemetryBuilder openTelemetryBuilder, OtelConfig otelConfig) AddOpenTelemetryApplication(this WebApplicationBuilder builder)
    {
        var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
        if (openTelemetryConfig is null)
            throw new NullReferenceException();
        builder.Services.Configure<OtelConfig>(builder.Configuration.GetSection("OpenTelemetry"));
        
        var openTelemetryBuilder = builder.Services.AddOpenTelemetry();
        
        // 샘플링 확률 설정
        if (!double.TryParse(openTelemetryConfig.TracesSamplerArg, out var probability))
        {
            probability = 1.0;
        }

        // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? openTelemetryConfig.Endpoint;
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? openTelemetryConfig.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? openTelemetryConfig.ServiceVersion;
        var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? openTelemetryConfig.ServiceNamespace;
        var deploymentEnvironment = Environment.GetEnvironmentVariable("OTEL_DEPLOYMENT_ENVIRONMENT") ?? openTelemetryConfig.DeploymentEnvironment;

        // 전체 OpenTelemetry 리소스 설정 - 더 많은 메타데이터 추가
        openTelemetryBuilder.ConfigureResource(resource => resource
            .AddService(
                serviceName: serviceName, 
                serviceVersion: serviceVersion,
                serviceInstanceId: openTelemetryConfig.ServiceInstanceId)
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.namespace"] = serviceNamespace,
                ["deployment.environment"] = deploymentEnvironment,
                ["host.name"] = Environment.MachineName,
                ["os.type"] = Environment.OSVersion.Platform.ToString(),
                ["os.description"] = Environment.OSVersion.VersionString,
                ["process.pid"] = Environment.ProcessId,
                ["process.executable.name"] = Assembly.GetExecutingAssembly().GetName().Name ?? "GamePulse",
                ["telemetry.sdk.name"] = "opentelemetry",
                ["telemetry.sdk.language"] = "dotnet",
                ["telemetry.sdk.version"] = typeof(OpenTelemetryBuilder).Assembly.GetName().Version?.ToString() ?? "unknown"
            }));

        // 추적 설정
        openTelemetryBuilder.WithTracing(tracing =>
        {
            tracing.AddSource(serviceName);
            tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
        });

        // 메트릭 설정
        openTelemetryBuilder.WithMetrics(metrics =>
        {
            // TelemetryService의 MeterName을 OpenTelemetry에 등록
            metrics.AddMeter(serviceName);
            
            // .NET 런타임 메트릭 추가
            metrics.AddMeter("System.Runtime");
        });

        // 로깅 설정은 Infrastructure 레이어에서 처리

        // TelemetryService 등록
        builder.Services.AddSingleton<ITelemetryService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TelemetryService>>();
            return new TelemetryService(serviceName, serviceVersion, logger);
        });

        // Tracer 서비스 등록
        openTelemetryBuilder.Services.AddSingleton(TracerProvider.Default.GetTracer(serviceName));

        return (builder, openTelemetryBuilder, openTelemetryConfig);
    }
}
