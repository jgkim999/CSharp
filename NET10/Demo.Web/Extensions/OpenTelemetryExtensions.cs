using Demo.Web.Configs;
using Demo.Application.Services;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;

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
    /// <returns>서비스 컬렉션</returns>
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // OpenTelemetry 구성 바인딩
        var otelConfig = new OpenTelemetryConfig();
        configuration.GetSection(OpenTelemetryConfig.SectionName).Bind(otelConfig);
        
        // 구성 객체를 DI 컨테이너에 등록
        services.Configure<OpenTelemetryConfig>(
            configuration.GetSection(OpenTelemetryConfig.SectionName));

        // TelemetryService 등록
        services.AddTelemetryService();

        // 환경 변수에서 서비스 정보 오버라이드
        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? otelConfig.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ?? otelConfig.ServiceVersion;
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? otelConfig.Environment;
        var serviceInstanceId = Environment.GetEnvironmentVariable("OTEL_SERVICE_INSTANCE_ID") ?? 
                               otelConfig.ServiceInstanceId ?? 
                               Environment.MachineName;

        // OpenTelemetry 서비스 등록
        services.AddOpenTelemetry()
            .ConfigureResource(resourceBuilder =>
            {
                resourceBuilder
                    .AddService(
                        serviceName: serviceName,
                        serviceVersion: serviceVersion,
                        serviceInstanceId: serviceInstanceId)
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = environment,
                        ["service.namespace"] = "Demo",
                        ["host.name"] = Environment.MachineName,
                        ["os.type"] = Environment.OSVersion.Platform.ToString(),
                        ["process.pid"] = Environment.ProcessId,
                        ["dotnet.version"] = Environment.Version.ToString()
                    });
            })
            .WithTracing(tracingBuilder =>
            {
                if (otelConfig.Tracing.Enabled)
                {
                    ConfigureTracing(tracingBuilder, otelConfig);
                }
            })
            .WithMetrics(metricsBuilder =>
            {
                if (otelConfig.Metrics.Enabled)
                {
                    ConfigureMetrics(metricsBuilder, otelConfig);
                }
            });

        return services;
    }

    /// <summary>
    /// 트레이싱 구성을 설정합니다.
    /// </summary>
    /// <param name="tracingBuilder">트레이싱 빌더</param>
    /// <param name="config">OpenTelemetry 구성</param>
    private static void ConfigureTracing(TracerProviderBuilder tracingBuilder, OpenTelemetryConfig config)
    {
        tracingBuilder
            // ASP.NET Core 자동 계측
            .AddAspNetCoreInstrumentation(options =>
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
            })
            
            // HTTP 클라이언트 자동 계측
            .AddHttpClientInstrumentation(options =>
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
            })

            // Npgsql (PostgreSQL) 자동 계측
            .AddNpgsql()

            // 사용자 정의 ActivitySource 등록
            .AddSource(TelemetryService.ActivitySource.Name)
            .AddSource("Demo.Application")
            .AddSource("Demo.Infra")

            // 샘플링 구성
            .SetSampler(new TraceIdRatioBasedSampler(config.Tracing.SamplingRatio))

            // 리소스 제한 설정
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(config.ServiceName, config.ServiceVersion))

            // 익스포터 구성
            .AddExporters(config);
    }

    /// <summary>
    /// 메트릭 구성을 설정합니다.
    /// </summary>
    /// <param name="metricsBuilder">메트릭 빌더</param>
    /// <param name="config">OpenTelemetry 구성</param>
    private static void ConfigureMetrics(MeterProviderBuilder metricsBuilder, OpenTelemetryConfig config)
    {
        metricsBuilder
            // ASP.NET Core 메트릭
            .AddAspNetCoreInstrumentation()
            
            // HTTP 클라이언트 메트릭
            .AddHttpClientInstrumentation()
            
            // .NET 런타임 메트릭
            .AddRuntimeInstrumentation()
            
            // 프로세스 메트릭
            .AddProcessInstrumentation()

            // 사용자 정의 메터 추가
            .AddMeter(TelemetryService.Meter.Name)
            .AddMeter("Demo.Application")
            .AddMeter("Demo.Infra")

            // 메트릭 리더 구성
            .AddReader(new PeriodicExportingMetricReader(
                exporter: CreateMetricExporter(config),
                exportIntervalMilliseconds: config.Metrics.BatchExportIntervalMilliseconds))

            // 뷰 구성 (히스토그램 버킷 사용자 정의)
            .AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
            })
            .AddView("http.client.request.duration", new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new double[] { 0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10 }
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
        switch (config.Exporter.Type.ToLowerInvariant())
        {
            case "console":
                tracingBuilder.AddConsoleExporter(options =>
                {
                    options.Targets = ConsoleExporterOutputTargets.Console;
                });
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
                // 기본값으로 콘솔 익스포터 사용
                tracingBuilder.AddConsoleExporter();
                break;
        }

        return tracingBuilder;
    }

    /// <summary>
    /// 메트릭 익스포터를 생성합니다.
    /// </summary>
    /// <param name="config">OpenTelemetry 구성</param>
    /// <returns>메트릭 익스포터</returns>
    private static BaseExporter<Metric> CreateMetricExporter(OpenTelemetryConfig config)
    {
        return config.Exporter.Type.ToLowerInvariant() switch
        {
            "console" => new ConsoleMetricExporter(new ConsoleExporterOptions
            {
                Targets = ConsoleExporterOutputTargets.Console
            }),
            
            "otlp" when !string.IsNullOrEmpty(config.Exporter.OtlpEndpoint) => 
                new OtlpMetricExporter(new OtlpExporterOptions
                {
                    Endpoint = new Uri(config.Exporter.OtlpEndpoint),
                    Protocol = config.Exporter.OtlpProtocol.ToLowerInvariant() switch
                    {
                        "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
                        "grpc" => OtlpExportProtocol.Grpc,
                        _ => OtlpExportProtocol.Grpc
                    },
                    TimeoutMilliseconds = config.Exporter.TimeoutMilliseconds
                }),
            
            _ => new ConsoleMetricExporter(new ConsoleExporterOptions
            {
                Targets = ConsoleExporterOutputTargets.Console
            })
        };
    }
}