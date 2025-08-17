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
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;

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
    /// <param name="logger"></param>
    /// <returns>구성된 OpenTelemetryBuilder</returns>
    public static (WebApplicationBuilder builder, OpenTelemetryBuilder openTelemetryBuilder, OtelConfig otelConfig) 
        AddOpenTelemetryApplication(this WebApplicationBuilder builder, Serilog.ILogger logger)
    {
        var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
        if (openTelemetryConfig is null)
            throw new NullReferenceException();
        builder.Services.Configure<OtelConfig>(builder.Configuration.GetSection("OpenTelemetry"));
        logger.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", openTelemetryConfig.Endpoint);
        
        var openTelemetryBuilder = builder.Services.AddOpenTelemetry();
        
        // 샘플링 확률 설정
        if (!double.TryParse(openTelemetryConfig.TracesSamplerArg, out var probability))
        {
            probability = 1.0;
        }

        // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? openTelemetryConfig.Endpoint;
        logger.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", otlpEndpoint);
        
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? openTelemetryConfig.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? openTelemetryConfig.ServiceVersion;
        var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ?? openTelemetryConfig.ServiceNamespace;
        var deploymentEnvironment = Environment.GetEnvironmentVariable("OTEL_DEPLOYMENT_ENVIRONMENT") ?? openTelemetryConfig.DeploymentEnvironment;

        // 전체 OpenTelemetry 리소스 설정 - 더 많은 메타데이터 추가
        openTelemetryBuilder.ConfigureResource(resource =>
        {
            resource.AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: openTelemetryConfig.ServiceInstanceId);
            resource.AddAttributes(new Dictionary<string, object>
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
                ["telemetry.sdk.version"] = typeof(OpenTelemetryBuilder).Assembly.GetName().Version?.ToString() ??
                                            "unknown"
            });
        });

        // 추적 설정
        openTelemetryBuilder.WithTracing(tracing =>
        {
            ConfigureTrace(tracing, serviceName, probability, otlpEndpoint);
        });

        // 메트릭 설정
        openTelemetryBuilder.WithMetrics(metrics =>
        {
            ConfigureMetric(metrics, serviceName, otlpEndpoint);
        });

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

    private static void ConfigureMetric(MeterProviderBuilder metrics, string serviceName, string openTelemetryEndpoint)
    {
        // TelemetryService의 MeterName을 OpenTelemetry에 등록
        metrics.AddMeter(serviceName);
        // ASP.NET Core 메트릭
        metrics.AddAspNetCoreInstrumentation();
        // HTTP 클라이언트 메트릭
        metrics.AddHttpClientInstrumentation();
        // .NET 런타임 메트릭
        metrics.AddRuntimeInstrumentation();
        // 프로세스 메트릭
        metrics.AddProcessInstrumentation();
        // Metrics provides by ASP.NET Core
        metrics.AddMeter("Microsoft.AspNetCore.Hosting");
        metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        // .NET 런타임 메트릭 추가
        metrics.AddMeter("System.Runtime");
        metrics.AddMeter("System.Net.Http");
        metrics.AddMeter("System.Net.NameResolution");
        // TODO: 사용자 정의 메터 추가
        //metrics.AddMeter(meterName);
        // 뷰 구성 (히스토그램 버킷 사용자 정의)
        metrics.AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        });
        metrics.AddView("http.client.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        });

        metrics.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(openTelemetryEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        });
    }

    private static void ConfigureTrace(TracerProviderBuilder tracing, string serviceName, double probability, string openTelemetryEndpoint)
    {
        tracing.AddSource(serviceName);
        // TODO: 사용자 정의 ActivitySource 등록
        
        //tracing.AddSource("Demo.Application");
        //tracing.AddSource("Demo.Infra");
        tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
        // ASP.NET Core 자동 계측
        tracing.AddAspNetCoreInstrumentation(options =>
        {
            // HTTP 요청 필터링 (헬스체크 등 제외)
            options.Filter = context =>
            {
                var path = context.Request.Path.Value?.ToLowerInvariant();
                return !string.IsNullOrEmpty(path) &&
                       !path.Contains("/health") &&
                       !path.Contains("/metrics") &&
                       !path.Contains("/favicon.ico");
            };

            // 요청 및 응답 세부 정보 수집
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.request.method", request.Method);
                activity.SetTag("http.request.scheme", request.Scheme);
                activity.SetTag("http.request.host", request.Host.Value);
                activity.SetTag("user_agent", request.Headers.UserAgent.ToString());

                // 사용자 정의 헤더 추가 (필요시)
                if (request.Headers.ContainsKey("X-Request-ID"))
                {
                    activity.SetTag("request.id", request.Headers["X-Request-ID"].ToString());
                }
            };

            options.EnrichWithHttpResponse = (activity, response) =>
            {
                activity.SetTag("http.response.status_code", response.StatusCode);
                activity.SetTag("http.response.content_length", response.ContentLength);
            };
        });

        // HTTP 클라이언트 자동 계측
        tracing.AddHttpClientInstrumentation(options =>
        {
            // 외부 API 호출 필터링
            options.FilterHttpRequestMessage = request =>
            {
                var uri = request.RequestUri?.ToString().ToLowerInvariant();
                return !string.IsNullOrEmpty(uri) &&
                       !uri.Contains("/health") &&
                       !uri.Contains("/metrics");
            };

            options.RecordException = true;
            options.EnrichWithHttpRequestMessage = (activity, request) =>
            {
                activity.SetTag("http.client.method", request.Method.Method);
                activity.SetTag("http.client.url", request.RequestUri?.ToString());
            };

            options.EnrichWithHttpResponseMessage = (activity, response) =>
            {
                activity.SetTag("http.client.status_code", (int)response.StatusCode);
                activity.SetTag("http.client.response_size", response.Content.Headers.ContentLength);
            };
        });

        // 데이터베이스 자동 계측
        tracing.AddNpgsql(); // PostgreSQL (Npgsql) 계측
        tracing.AddSqlClientInstrumentation(options =>
        {
            // SQL 명령문 텍스트 기록 (개발 환경에서만)
            options.SetDbStatementForText = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            options.RecordException = true;
        });

        tracing.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(openTelemetryEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        });
    }
}
