using Demo.Application.Configs;
using Demo.Infra.Services;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Demo.Infra.Extensions;

/// <summary>
/// OpenTelemetry Infrastructure 레이어 설정을 위한 확장 메서드
/// </summary>
public static class OpenTelemetryInfraExtensions
{
    /// <summary>
    /// OpenTelemetry의 인프라 관련 계측 및 익스포터를 구성합니다
    /// </summary>
    /// <param name="builder">OpenTelemetryBuilder 인스턴스</param>
    /// <param name="config">OpenTelemetry 구성 설정</param>
    /// <summary>
    /// Configures OpenTelemetry tracing and metrics instrumentation and exporters for the infrastructure layer.
    /// </summary>
    /// <remarks>
    /// Initializes the GamePulseActivitySource with the service name and version from <paramref name="config"/>.
    /// Tracing: enables ASP.NET Core, HttpClient and Redis instrumentation, and adds an OTLP exporter using the endpoint and gRPC protocol from <paramref name="config"/>.
    /// Metrics: enables ASP.NET Core, Runtime and HttpClient instrumentation, registers common ASP.NET and System.Net meters, and adds an OTLP exporter with a 5000 ms metric export interval.
    /// If Redis instrumentation is created, it is registered into the DI container as a singleton.
    /// </remarks>
    /// <param name="config">OpenTelemetry configuration containing at least Endpoint, ServiceName and ServiceVersion.</param>
    /// <returns>The same <see cref="OpenTelemetryBuilder"/> instance for chaining.</returns>
    public static OpenTelemetryBuilder AddOpenTelemetryInfrastructure(this OpenTelemetryBuilder builder, OtelConfig config)
    {
        StackExchangeRedisInstrumentation? redisInstrumentation = null;

        // GamePulseActivitySource 초기화
        GamePulseActivitySource.Initialize(config.ServiceName, config.ServiceVersion);

        // 추적 인프라 설정
        builder.WithTracing(tracing =>
        {
            // ASP.NET Core 및 HTTP 클라이언트 계측
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            
            // Redis 계측
            tracing.AddRedisInstrumentation()
                .ConfigureRedisInstrumentation(instrumentation => redisInstrumentation = instrumentation);
            
            // OTLP 익스포터
            tracing.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(config.Endpoint);
                o.Protocol = OtlpExportProtocol.Grpc;
            });
            
            // 개발 시 콘솔 익스포터 (주석 처리됨)
            // tracing.AddConsoleExporter();
        });

        // Redis 계측 서비스 등록
        if (redisInstrumentation is not null)
        {
            builder.Services.AddSingleton(redisInstrumentation);
        }

        // 메트릭 인프라 설정
        builder.WithMetrics(metrics =>
        {
            // ASP.NET Core 및 런타임 계측
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddRuntimeInstrumentation();
            metrics.AddHttpClientInstrumentation();
            
            // ASP.NET Core에서 제공하는 메트릭 (.NET 8)
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            
            // System.Net 라이브러리에서 제공하는 메트릭
            metrics.AddMeter("System.Net.Http");
            metrics.AddMeter("System.Net.NameResolution");
            
            // OTLP 익스포터
            metrics.AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = new Uri(config.Endpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000; // 5초로 설정
            });
            
            // 개발 시 콘솔 익스포터 (주석 처리됨)
            // metrics.AddConsoleExporter();
        });

        // 로깅 설정 (주석 처리됨)
        /*
        builder.WithLogging(logging => logging
            .AddConsoleExporter());
        */

        return builder;
    }
}