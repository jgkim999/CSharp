using System.Diagnostics;

using Demo.Application.Services;
using Demo.Web.Configs;
using Microsoft.Extensions.Options;
using Npgsql;

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Demo.Web.Extensions;

/// <summary>
/// OpenTelemetry 서비스 등록을 위한 확장 메서드 클래스
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// OpenTelemetry 서비스를 DI 컨테이너에 등록합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">구성 객체</param>
    /// <param name="serviceInstanceId"></param>
    /// <param name="environment"></param>
    /// <summary>
    /// Registers and configures OpenTelemetry tracing and metrics services in the dependency injection container using the provided configuration, service instance ID, and environment.
    /// </summary>
    /// <param name="services">The service collection to which OpenTelemetry services will be added.</param>
    /// <param name="configuration">The application configuration containing OpenTelemetry settings.</param>
    /// <param name="serviceInstanceId">A unique identifier for the service instance.</param>
    /// <param name="environment">The current application environment (e.g., Development, Production).</param>
    /// <returns>The updated service collection with OpenTelemetry services registered.</returns>
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceInstanceId,
        string environment)
    {
        // OpenTelemetry 구성 바인딩
        var otelConfig = new OpenTelemetryConfig();
        configuration.GetSection(OpenTelemetryConfig.SectionName).Bind(otelConfig);

        // 구성 객체를 DI 컨테이너에 등록
        services.Configure<OpenTelemetryConfig>(configuration.GetSection(OpenTelemetryConfig.SectionName));

        // TelemetryService 등록
        var telemetryService = new TelemetryService(otelConfig.ServiceName, otelConfig.ServiceVersion);
        services.AddSingleton<ITelemetryService>(telemetryService);

        // OpenTelemetry 서비스 등록
        var openTelemetryBuilder = services.AddOpenTelemetry();
        openTelemetryBuilder.ConfigureResource(resourceBuilder =>
        {
            resourceBuilder
                .AddService(
                    serviceName: otelConfig.ServiceName,
                    serviceVersion: otelConfig.ServiceVersion,
                    serviceInstanceId: serviceInstanceId)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.namespace"] = "Api",
                    ["host.name"] = Environment.MachineName,
                    ["os.type"] = Environment.OSVersion.Platform.ToString(),
                    ["process.pid"] = Environment.ProcessId,
                    ["dotnet.version"] = Environment.Version.ToString()
                });
        });
        openTelemetryBuilder.WithTracing(tracingBuilder =>
        {
            if (otelConfig.Tracing.Enabled)
            {
                ConfigureTracing(tracingBuilder, otelConfig, telemetryService.ActiveSourceName);
            }
        });
            
        openTelemetryBuilder.WithMetrics(metricsBuilder =>
        {
            if (otelConfig.Metrics.Enabled)
            {
                ConfigureMetrics(metricsBuilder, otelConfig, telemetryService.MeterName);
            }
        });

        return services;
    }

    /// <summary>
    /// 트레이싱 구성을 설정합니다.
    /// </summary>
    /// <param name="tracingBuilder">트레이싱 빌더</param>
    /// <summary>
    /// Configures tracing instrumentation and exporters for OpenTelemetry, including ASP.NET Core, HTTP client, and database tracing, with environment-based sampling and resource attributes.
    /// </summary>
    /// <param name="tracingBuilder">The builder used to configure tracing providers.</param>
    /// <param name="config">The OpenTelemetry configuration settings.</param>
    /// <param name="serviceName">The name of the service to use for custom ActivitySource registration.</param>
    private static void ConfigureTracing(TracerProviderBuilder tracingBuilder, OpenTelemetryConfig config, string serviceName)
    {
        // ASP.NET Core 자동 계측
        tracingBuilder.AddAspNetCoreInstrumentation(options =>
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
        tracingBuilder.AddHttpClientInstrumentation(options =>
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
        tracingBuilder.AddNpgsql(); // PostgreSQL (Npgsql) 계측
        tracingBuilder.AddSqlClientInstrumentation(options =>
        {
            // SQL 명령문 텍스트 기록 (개발 환경에서만)
            options.SetDbStatementForText = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            options.RecordException = true;
        });

        // 사용자 정의 ActivitySource 등록
        tracingBuilder.AddSource(serviceName);
        tracingBuilder.AddSource("Demo.Application");
        tracingBuilder.AddSource("Demo.Infra");

        // 환경별 샘플링 전략 구성
        tracingBuilder.SetSampler(SamplingStrategies.CreateEnvironmentBasedSampler(
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? config.Environment,
            config.Tracing.SamplingRatio));

        // 리소스 제한 설정
        tracingBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(config.ServiceName, config.ServiceVersion));

        // 익스포터 구성
        tracingBuilder.AddExporters(config);
    }

    /// <summary>
    /// 메트릭 구성을 설정합니다.
    /// </summary>
    /// <param name="metricsBuilder">메트릭 빌더</param>
    /// <summary>
    /// Configures OpenTelemetry metrics instrumentation, meters, histogram views, and OTLP exporter for the application.
    /// </summary>
    /// <param name="metricsBuilder">The builder used to configure metrics providers and instrumentation.</param>
    /// <param name="config">The OpenTelemetry configuration settings.</param>
    /// <param name="meterName">The name of the custom meter to register for application metrics.</param>
    private static void ConfigureMetrics(MeterProviderBuilder metricsBuilder, OpenTelemetryConfig config, string meterName)
    {
        // ASP.NET Core 메트릭
        metricsBuilder.AddAspNetCoreInstrumentation();
        // HTTP 클라이언트 메트릭
        metricsBuilder.AddHttpClientInstrumentation();
        // .NET 런타임 메트릭
        metricsBuilder.AddRuntimeInstrumentation();
        // 프로세스 메트릭
        metricsBuilder.AddProcessInstrumentation();
        // Metrics provides by ASP.NET Core in .NET 8
        metricsBuilder.AddMeter("Microsoft.AspNetCore.Hosting");
        metricsBuilder.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        // Metrics provided by System.Net libraries
        metricsBuilder.AddMeter("System.Net.Http");
        metricsBuilder.AddMeter("System.Net.NameResolution");
        // 사용자 정의 메터 추가
        metricsBuilder.AddMeter(meterName);
        metricsBuilder.AddMeter("Demo.Application");
        metricsBuilder.AddMeter("Demo.Infra");

        // 환경별 메트릭 리더 구성
        //.AddReader(MetricProcessingStrategies.CreateEnvironmentBasedMetricReader(
        //    config, 
        //    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? config.Environment))

        // 뷰 구성 (히스토그램 버킷 사용자 정의)
        metricsBuilder.AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        });
        metricsBuilder.AddView("http.client.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        });
        metricsBuilder.AddOtlpExporter(options =>
        {
            Debug.Assert(config.Exporter.OtlpEndpoint != null, "config.Exporter.OtlpEndpoint != null");
            options.Endpoint = new Uri(config.Exporter.OtlpEndpoint);
            options.Protocol = OtlpExportProtocol.Grpc;
        });
    }

    /// <summary>
    /// 트레이싱 익스포터를 추가합니다.
    /// </summary>
    /// <param name="tracingBuilder">트레이싱 빌더</param>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>트레이싱 빌더</returns>
    private static TracerProviderBuilder AddExporters(this TracerProviderBuilder tracingBuilder, OpenTelemetryConfig config)
    {
        var exporterType = config.Exporter.Type.ToLowerInvariant();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        // 개발 환경에서는 항상 콘솔 익스포터도 추가
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            tracingBuilder.AddConsoleExporter(options =>
            {
                options.Targets = ConsoleExporterOutputTargets.Console;
            });
        }

        switch (exporterType)
        {
            case "console":
                // 이미 개발 환경에서 추가했으므로, 다른 환경에서만 추가
                if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    tracingBuilder.AddConsoleExporter(options =>
                    {
                        options.Targets = ConsoleExporterOutputTargets.Console;
                    });
                }
                break;

            case "otlp":
                if (!string.IsNullOrEmpty(config.Exporter.OtlpEndpoint))
                {
                    tracingBuilder.AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(config.Exporter.OtlpEndpoint);
                        options.Protocol = config.Exporter.OtlpProtocol.ToLowerInvariant() switch
                        {
                            "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                            "grpc" => OtlpExportProtocol.Grpc,
                            _ => OtlpExportProtocol.Grpc
                        };
                        options.TimeoutMilliseconds = config.Exporter.TimeoutMilliseconds;

                        // 헤더 설정
                        if (config.Exporter.OtlpHeaders.Count > 0)
                        {
                            var headers = string.Empty;
                            foreach (var header in config.Exporter.OtlpHeaders)
                            {
                                headers += $"{header.Key}={header.Value},";
                            }
                            options.Headers = headers.TrimEnd(',');
                        }
                    });
                }
                break;

            case "jaeger":
                // Jaeger는 OTLP를 통해 지원됨
                tracingBuilder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(config.Exporter.OtlpEndpoint ?? "http://localhost:14268/api/traces");
                    options.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
                break;

            default:
                // 기본값으로 콘솔 익스포터 사용 (개발 환경이 아닌 경우에만)
                if (!environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                {
                    tracingBuilder.AddConsoleExporter();
                }
                break;
        }

        return tracingBuilder;
    }
}