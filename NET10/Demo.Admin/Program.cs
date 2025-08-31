using System.Globalization;
using System.Reflection;
using Blazored.LocalStorage;
using MudBlazor.Services;
using Demo.Admin.Components;
using Demo.Application.Configs;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using OpenTelemetryBuilder = OpenTelemetry.OpenTelemetryBuilder;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults 추가
builder.AddServiceDefaults();

// 환경별 설정 파일 추가
var environment = builder.Environment.EnvironmentName;
var environmentFromEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.IsNullOrWhiteSpace(environmentFromEnv) == false)
    environment = environmentFromEnv;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    #region SeriLog
    builder.Host.UseSerilog();
    builder.Services.AddSerilog((services, lc) =>
    {
        lc.ReadFrom.Configuration(builder.Configuration);
        lc.ReadFrom.Services(services);
    });
    #endregion
    
    Log.Information("Starting application");
    
    #region OpenTelemetry
    var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
    if (openTelemetryConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<OtelConfig>(builder.Configuration.GetSection("OpenTelemetry"));
    Log.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", openTelemetryConfig.Endpoint);
    
    var openTelemetryBuilder = builder.Services.AddOpenTelemetry();
    
    // 샘플링 확률 설정 - InvariantCulture 사용 및 0~1 범위로 제한
    if (!double.TryParse(openTelemetryConfig.TracesSamplerArg, NumberStyles.Float, CultureInfo.InvariantCulture, out var probability))
    {
        probability = 1.0;
    }
    else
    {
        // 0~1 범위로 제한
        probability = Math.Clamp(probability, 0.0, 1.0);
    }

    // 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? openTelemetryConfig.Endpoint;
    Log.Information("OpenTelemetryEndpoint {OpenTelemetryEndpoint}", otlpEndpoint);
    
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
    #endregion

    #region RabbitMQ
    var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
    if (rabbitMqConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMqPublishService, RabbitMqPublishService>();
    #endregion

    #region Redis
    var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));
    #endregion
    
    #region PgSql
    var postgresConfig = builder.Configuration.GetSection("Postgres").Get<PostgresConfig>();
    if (postgresConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<PostgresConfig>(builder.Configuration.GetSection("Postgres"));
    builder.Services.AddDbContextFactory<DemoDbContext>(options =>
        options.UseNpgsql(postgresConfig.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(10); // 명령 타임아웃 10초로 제한
        }));
    #endregion
    
    builder.Services.AddBlazoredLocalStorage();
    // Add MudBlazor services
    builder.Services.AddMudServices();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    
    // Graceful shutdown 설정
    builder.Services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(10); // 기본 30초에서 10초로 단축
    });
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();


    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MapDefaultEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

void ConfigureTrace(TracerProviderBuilder tracing, string serviceName, double probability, string openTelemetryEndpoint)
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
        builder.Services.AddSingleton(redisInstrumentation);
    }

    // OTLP exporter는 ServiceDefaults에서 UseOtlpExporter()로 자동 구성됨
}

void ConfigureMetric(MeterProviderBuilder metrics, string serviceName, string openTelemetryEndpoint)
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

    // OTLP exporter는 ServiceDefaults에서 UseOtlpExporter()로 자동 구성됨
}
