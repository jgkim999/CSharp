using System.Globalization;
using Demo.Application.Configs;
using Demo.Application.Services;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Demo.Admin;

public static class OpenTelemetryInitialize
{
    public static void AddOpenTelemetryApplication(this WebApplicationBuilder appBuilder, Serilog.ILogger logger)
    {
        var openTelemetryConfig = appBuilder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
        if (openTelemetryConfig is null)
            throw new NullReferenceException();
        appBuilder.Services.Configure<OtelConfig>(appBuilder.Configuration.GetSection("OpenTelemetry"));

        Log.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", openTelemetryConfig.Endpoint);

        appBuilder.Logging.AddOpenTelemetry(logger =>
        {
            logger.IncludeFormattedMessage = true;
            logger.IncludeScopes = true;
        });
        
        var openTelemetryBuilder = appBuilder.Services.AddOpenTelemetry();

        // 샘플링 확률 설정 - InvariantCulture 사용 및 0~1 범위로 제한
        if (!double.TryParse(openTelemetryConfig.TracesSamplerArg, NumberStyles.Float, CultureInfo.InvariantCulture,
                out var probability))
        {
            probability = 1.0;
        }
        else
        {
            // 0~1 범위로 제한
            probability = Math.Clamp(probability, 0.0, 1.0);
        }

        // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ??
                           openTelemetryConfig.Endpoint;
        Log.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", otlpEndpoint);

        var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? openTelemetryConfig.ServiceName;
        var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION") ??
                             openTelemetryConfig.ServiceVersion;
        var serviceNamespace = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAMESPACE") ??
                               openTelemetryConfig.ServiceNamespace;
        var deploymentEnvironment = Environment.GetEnvironmentVariable("OTEL_DEPLOYMENT_ENVIRONMENT") ??
                                    openTelemetryConfig.DeploymentEnvironment;

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
                //["telemetry.sdk.version"] = typeof(OpenTelemetryBuilder).Assembly.GetName().Version?.ToString() ?? "unknown"
            });
        });

        // 추적 설정
        openTelemetryBuilder.WithTracing(tracing =>
        {
            ConfigureTrace(appBuilder, tracing, serviceName, probability, otlpEndpoint);
        });

        // 메트릭 설정
        openTelemetryBuilder.WithMetrics(metrics =>
        {
            ConfigureMetric(metrics, serviceName, otlpEndpoint);
        });

        openTelemetryBuilder.UseOtlpExporter(
            OpenTelemetry.Exporter.OtlpExportProtocol.Grpc,
            new Uri(otlpEndpoint));

        // TelemetryService 등록
        appBuilder.Services.AddSingleton<ITelemetryService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<TelemetryService>>();
            return new TelemetryService(serviceName, serviceVersion, logger);
        });
    }

    static void ConfigureTrace(WebApplicationBuilder appBuilder, TracerProviderBuilder tracing, string serviceName, double probability, string openTelemetryEndpoint)
    {
        tracing.AddSource(serviceName);
        // Demo.Admin의 사용자 정의 ActivitySource 등록
        tracing.AddSource("Demo.Admin");
        tracing.AddSource("Demo.Application");
        tracing.AddSource("Demo.Infra");
        tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
        // ASP.NET Core 자동 계측 - Blazor Server 최적화
        /*
        tracing.AddAspNetCoreInstrumentation(options =>
        {
            // HTTP 요청 필터링 (헬스체크, SignalR ComponentHub 등 제외)
            options.Filter = context =>
            {
                var path = context.Request.Path.Value?.ToLowerInvariant();
                
                // 제외할 경로들
                if (string.IsNullOrEmpty(path) ||
                    path.Contains("/health") ||
                    path.Contains("/metrics") ||
                    path.Contains("/favicon.ico") ||
                    path.Contains("/_blazor/negotiate") ||
                    path.Contains("/_blazor/disconnect") ||
                    path.StartsWith("/_blazor/"))
                {
                    return false;
                }
                
                // SignalR ComponentHub 관련 헤더 확인
                var headers = context.Request.Headers;
                if (headers.ContainsKey("Connection") && 
                    headers["Connection"].ToString().Contains("Upgrade"))
                {
                    return false; // WebSocket 연결 요청 제외
                }
                
                // User-Agent에서 SignalR 클라이언트 확인
                if (headers.ContainsKey("User-Agent"))
                {
                    var userAgent = headers["User-Agent"].ToString().ToLowerInvariant();
                    if (userAgent.Contains("signalr") || 
                        userAgent.Contains("microsoft.aspnetcore.signalr.client"))
                    {
                        return false;
                    }
                }
                
                // ComponentHub 관련 요청 제외
                if (headers.ContainsKey("X-Requested-With") && 
                    headers["X-Requested-With"].ToString().Contains("XMLHttpRequest"))
                {
                    // Blazor Server ComponentHub의 AJAX 요청들 제외
                    if (path?.Contains("/_blazor") == true)
                    {
                        return false;
                    }
                }
                
                // Content-Type으로 ComponentHub 호출 감지
                if (headers.ContainsKey("Content-Type"))
                {
                    var contentType = headers["Content-Type"].ToString().ToLowerInvariant();
                    if (contentType.Contains("application/x-msgpack") || 
                        contentType.Contains("text/plain; charset=utf-8"))
                    {
                        return false; // SignalR ComponentHub 메시지 제외
                    }
                }
                
                return true;
            };

            // 요청 및 응답 세부 정보 수집
            options.RecordException = true;
            options.EnrichWithHttpRequest = (activity, request) =>
            {
                activity.SetTag("http.request.method", request.Method);
                activity.SetTag("http.request.scheme", request.Scheme);
                activity.SetTag("http.request.host", request.Host.Value);
                activity.SetTag("user_agent", request.Headers.UserAgent.ToString());

                var path = request.Path.Value;
                activity.SetTag("http.request.path", path);
                
                // Blazor Server SignalR 요청 식별 및 개선
                if (path?.Contains("/_blazor") == true)
                {
                    activity.SetTag("blazor.signalr", "true");
                    activity.SetTag("blazor.transport", "signalr");
                    
                    // Referer 헤더에서 실제 페이지 경로 추출
                    if (request.Headers.ContainsKey("Referer"))
                    {
                        var referer = request.Headers["Referer"].ToString();
                        if (referer.Contains("/servertime"))
                        {
                            activity.SetTag("blazor.page", "/servertime");
                            activity.SetTag("blazor.component", "ServerTime");
                            // 더 의미있는 operation name 설정
                            activity.DisplayName = "ServerTime Page - Blazor SignalR";
                        }
                    }
                    
                    // Query string에서 추가 정보 추출
                    if (request.Query.ContainsKey("id"))
                    {
                        activity.SetTag("blazor.circuit_id", request.Query["id"].ToString());
                    }
                }
                // 일반 페이지 요청
                else if (!string.IsNullOrEmpty(path))
                {
                    if (path.Equals("/servertime", StringComparison.OrdinalIgnoreCase))
                    {
                        activity.SetTag("blazor.page", "/servertime");
                        activity.SetTag("blazor.component", "ServerTime");
                        activity.SetTag("request.type", "page_load");
                        activity.DisplayName = "ServerTime Page - Initial Load";
                    }
                }

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
        */
        // HTTP 클라이언트 자동 계측 - RestSharp 호출 포함
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
                activity.SetTag("http.client.host", request.RequestUri?.Host);
                activity.SetTag("http.client.port", request.RequestUri?.Port);
                activity.SetTag("http.client.scheme", request.RequestUri?.Scheme);
                
                // RestSharp 호출 식별
                if (request.Headers.UserAgent?.ToString().Contains("Demo.Admin") == true)
                {
                    activity.SetTag("http.client.library", "RestSharp");
                    activity.SetTag("http.client.component", "Demo.Admin");
                }
                
                // Request headers 추가 (필요시)
                foreach (var header in request.Headers)
                {
                    if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                    {
                        activity.SetTag("http.request.header.accept", string.Join(",", header.Value));
                    }
                }
            };

            options.EnrichWithHttpResponseMessage = (activity, response) =>
            {
                activity.SetTag("http.client.status_code", (int)response.StatusCode);
                activity.SetTag("http.client.status_text", response.ReasonPhrase);
                activity.SetTag("http.client.response_size", response.Content.Headers.ContentLength ?? 0);
                
                // Response headers 추가
                if (response.Headers.Contains("Content-Type"))
                {
                    activity.SetTag("http.response.header.content_type", 
                        string.Join(",", response.Headers.GetValues("Content-Type")));
                }
            };
        });

        // 데이터베이스 자동 계측
        tracing.AddNpgsql(); // PostgreSQL (Npgsql) 계측
        
        tracing.AddRabbitMQInstrumentation();
        
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
        
        // Redis 계측 서비스 등록
        if (redisInstrumentation is not null)
        {
            appBuilder.Services.AddSingleton(redisInstrumentation);
        }

        // OTLP exporter는 ServiceDefaults에서 UseOtlpExporter()로 자동 구성됨
    }

    static void ConfigureMetric(MeterProviderBuilder metrics, string serviceName, string openTelemetryEndpoint)
    {
        // TelemetryService의 MeterName을 OpenTelemetry에 등록
        metrics.AddMeter(serviceName);
        // Demo.Admin 사용자 정의 메터 추가
        metrics.AddMeter("Demo.Admin");
        // ASP.NET Core 메트릭
        metrics.AddAspNetCoreInstrumentation();
        // HTTP 클라이언트 메트릭 - RestSharp 호출 포함
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
        // 뷰 구성 (히스토그램 버킷 사용자 정의)
        metrics.AddView("http.server.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        });
        metrics.AddView("http.client.request.duration", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
        });
        // OTLP exporter는 ServiceDefaults에서 UseOtlpExporter()로 자동 구성됨
    }
}