using Demo.Application.Configs;
using Demo.Infra.Services;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
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
    /// <returns>구성된 OpenTelemetryBuilder</returns>
    public static OpenTelemetryBuilder AddOpenTelemetryInfrastructure(this OpenTelemetryBuilder builder, OtelConfig config)
    {
        StackExchangeRedisInstrumentation? redisInstrumentation = null;

        // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? config.Endpoint;
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? config.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? config.ServiceVersion;

        // GamePulseActivitySource 초기화
        GamePulseActivitySource.Initialize(serviceName, serviceVersion);

        // 추적 인프라 설정
        builder.WithTracing(tracing =>
        {
            // ASP.NET Core 계측 - 더 상세한 설정
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, httpRequest) =>
                {
                    activity.SetTag("http.request.body.size", httpRequest.ContentLength);
                    activity.SetTag("http.request.header.user_agent", httpRequest.Headers.UserAgent.ToString());
                    activity.SetTag("http.request.header.host", httpRequest.Host.ToString());
                };
                options.EnrichWithHttpResponse = (activity, httpResponse) =>
                {
                    activity.SetTag("http.response.body.size", httpResponse.ContentLength);
                };
            });
            
            // HTTP 클라이언트 계측 - 더 상세한 설정
            tracing.AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                {
                    activity.SetTag("http.request.method", httpRequestMessage.Method.ToString());
                    activity.SetTag("http.request.uri", httpRequestMessage.RequestUri?.ToString());
                };
                options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                {
                    activity.SetTag("http.response.status_code", (int)httpResponseMessage.StatusCode);
                    activity.SetTag("http.response.content_length", httpResponseMessage.Content.Headers.ContentLength);
                };
            });
            
            // Redis 계측
            tracing.AddRedisInstrumentation()
                .ConfigureRedisInstrumentation(instrumentation => redisInstrumentation = instrumentation);
            
            // Entity Framework Core 계측은 별도 패키지가 필요하므로 주석 처리
            // 필요시 OpenTelemetry.Instrumentation.EntityFrameworkCore 패키지 추가 후 활성화
            
            // OTLP 익스포터 - 환경 변수 지원 및 배치 설정
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                options.TimeoutMilliseconds = config.BatchExportTimeoutMs;
            });
            
            // 개발 환경에서 콘솔 익스포터는 별도 패키지가 필요하므로 주석 처리
            // 필요시 OpenTelemetry.Exporter.Console 패키지의 최신 버전 사용
        });

        // Redis 계측 서비스 등록
        if (redisInstrumentation is not null)
        {
            builder.Services.AddSingleton(redisInstrumentation);
        }

        // 메트릭 인프라 설정
        builder.WithMetrics(metrics =>
        {
            // ASP.NET Core 계측
            metrics.AddAspNetCoreInstrumentation();
            
            // .NET 런타임 계측
            metrics.AddRuntimeInstrumentation();
            
            // HTTP 클라이언트 계측
            metrics.AddHttpClientInstrumentation();
            
            // 프로세스 메트릭은 별도 패키지가 필요하므로 주석 처리
            // 필요시 OpenTelemetry.Instrumentation.Process 패키지 추가
            
            // ASP.NET Core에서 제공하는 메트릭
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            metrics.AddMeter("Microsoft.AspNetCore.Http.Connections");
            metrics.AddMeter("Microsoft.AspNetCore.Routing");
            metrics.AddMeter("Microsoft.AspNetCore.Diagnostics");
            
            // System.Net 라이브러리에서 제공하는 메트릭
            metrics.AddMeter("System.Net.Http");
            metrics.AddMeter("System.Net.NameResolution");
            metrics.AddMeter("System.Net.Security");
            
            // Entity Framework Core 메트릭
            metrics.AddMeter("Microsoft.EntityFrameworkCore");
            
            // OTLP 익스포터 - 환경 변수 지원
            metrics.AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                options.TimeoutMilliseconds = config.BatchExportTimeoutMs;
                
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = config.MetricExportIntervalMs;
                readerOptions.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = config.BatchExportTimeoutMs;
            });
            
            // Prometheus 익스포터는 ASP.NET Core에서 별도로 설정
            // 콘솔 익스포터는 별도 패키지가 필요하므로 주석 처리
        });

        // 로깅은 Serilog를 통해 처리 (OpenTelemetry 로그 익스포터는 Serilog 설정에서 구성됨)

        return builder;
    }
}