using Demo.Application.Configs;
using Demo.Application.Services;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sdk = OpenTelemetry.Sdk;

namespace Demo.SimpleSocket;

/// <summary>
/// OpenTelemetry 텔레메트리 시스템 초기화를 담당하는 정적 클래스
/// 추적, 메트릭, 로깅을 포함한 포괄적인 관찰성(Observability) 설정을 제공합니다
/// </summary>
public static class OpenTelemetryInitializer
{
    /// <summary>
    /// WebApplicationBuilder에 OpenTelemetry 텔레메트리 시스템을 추가하고 구성합니다
    /// ASP.NET Core, HTTP 클라이언트, 데이터베이스, Redis, RabbitMQ 등의 자동 계측을 포함합니다
    /// </summary>
    /// <param name="appBuilder">웹 애플리케이션 빌더</param>
    /// <param name="logger">Serilog 로거 인스턴스</param>
    /// <exception cref="NullReferenceException">OpenTelemetry 구성이 null인 경우</exception>
    public static void AddOpenTelemetryApplication(this WebApplicationBuilder appBuilder, Serilog.ILogger logger)
    {
        var openTelemetryConfig = appBuilder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
        if (openTelemetryConfig is null)
            throw new NullReferenceException();
        appBuilder.Services.Configure<OtelConfig>(appBuilder.Configuration.GetSection("OpenTelemetry"));

        logger.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", openTelemetryConfig.Endpoint);

        var compositeTextMapPropagator = new CompositeTextMapPropagator(new TextMapPropagator[]
        {
            new TraceContextPropagator(),
            new BaggagePropagator()
        });

        Sdk.SetDefaultTextMapPropagator(compositeTextMapPropagator);

        appBuilder.Logging.AddOpenTelemetry(logger =>
        {
            logger.IncludeFormattedMessage = true;
            logger.IncludeScopes = true;
        });

        var openTelemetryBuilder = appBuilder.Services.AddOpenTelemetry();

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
                //["os.type"] = Environment.OSVersion.Platform.ToString(),
                //["os.description"] = Environment.OSVersion.VersionString,
                //["process.pid"] = Environment.ProcessId,
                //["process.executable.name"] = Assembly.GetExecutingAssembly().GetName().Name ?? "GamePulse",
                //["telemetry.sdk.name"] = "opentelemetry",
                //["telemetry.sdk.language"] = "dotnet",
                //["telemetry.sdk.version"] = typeof(OpenTelemetryBuilder).Assembly.GetName().Version?.ToString() ??"unknown"
            });
        });

        // 추적 설정
        openTelemetryBuilder.WithTracing(tracing =>
        {
            // 서비스 ActivitySource 등록
            tracing.AddSource(serviceName);
            // SuperSocket 관련 ActivitySource 등록
            tracing.AddSource("Session.*");
            tracing.AddSource("MessageHandler.*");
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

            StackExchangeRedisInstrumentation? redisInstrumentation = null;

            // 추적 인프라 설정
            // Redis 계측
            tracing.AddRedisInstrumentation()
                .ConfigureRedisInstrumentation(instrumentation => redisInstrumentation = instrumentation);

            // RabbitMQ 자동 instrumentation
            tracing.AddRabbitMQInstrumentation();

            // Redis 계측 서비스 등록
            if (redisInstrumentation is not null)
            {
                appBuilder.Services.AddSingleton(redisInstrumentation);
            }
        });

        // 메트릭 설정
        openTelemetryBuilder.WithMetrics(metrics =>
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
            // FusionCache 메트릭 추가
            metrics.AddMeter("Demo.Infra.FusionCache");
        });

        // TelemetryService 등록
        appBuilder.Services.AddSingleton<ITelemetryService>(provider =>
        {
            var telemetryLogger = provider.GetRequiredService<ILogger<TelemetryService>>();
            return new TelemetryService(serviceName, serviceVersion, telemetryLogger);
        });

        openTelemetryBuilder.UseOtlpExporter(
            OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
            new Uri(otlpEndpoint));
    }
}
