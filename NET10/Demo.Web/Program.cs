using Demo.Application;
using Demo.Infra;
using Demo.Web.Configs;
using Demo.Web.Endpoints.User;
using Demo.Web.Extensions;
using Demo.Web.Middleware;

using FastEndpoints;

using FluentValidation;

using Scalar.AspNetCore;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 환경별 설정 파일 추가
var environment = builder.Environment.EnvironmentName;
var environmentFromEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (string.IsNullOrWhiteSpace(environmentFromEnv) == false)
    environment = environmentFromEnv;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

// OpenTelemetry 설정 바인딩
var openTelemetryConfig = new OpenTelemetryConfig();
builder.Configuration.GetSection(OpenTelemetryConfig.SectionName).Bind(openTelemetryConfig);

// RateLimit 설정 바인딩
var rateLimitConfig = new RateLimitConfig();
builder.Configuration.GetSection(RateLimitConfig.SectionName).Bind(rateLimitConfig);

// 환경 변수에서 서비스 정보 오버라이드
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
if (string.IsNullOrWhiteSpace(serviceName) == false)
    openTelemetryConfig.ServiceName = serviceName;

var serviceVersion = Environment.GetEnvironmentVariable("OTEL_SERVICE_VERSION");
if (string.IsNullOrWhiteSpace(serviceVersion) == false)
    openTelemetryConfig.ServiceVersion = serviceVersion;

openTelemetryConfig.Environment = environment;

var serviceInstanceId = Environment.GetEnvironmentVariable("OTEL_SERVICE_INSTANCE_ID");
if (string.IsNullOrWhiteSpace(serviceInstanceId) == false)
{
    openTelemetryConfig.ServiceInstanceId = serviceInstanceId;
}
else
{
    openTelemetryConfig.ServiceInstanceId = Environment.MachineName;
}

// Serilog와 OpenTelemetry 통합 설정 (비동기)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithOpenTelemetry()
    .Enrich.WithProperty("ServiceName", openTelemetryConfig.ServiceName)
    .Enrich.WithProperty("ServiceVersion", openTelemetryConfig.ServiceVersion)
    .Enrich.WithProperty("Environment", openTelemetryConfig.Environment)
    .WriteTo.Async(a => a.OpenTelemetry(options =>
    {
        // OpenTelemetry 로깅이 활성화된 경우에만 설정
        if (openTelemetryConfig.Logging.Enabled)
        {
            // OTLP 엔드포인트가 설정된 경우 사용
            if (!string.IsNullOrEmpty(openTelemetryConfig.Exporter.OtlpEndpoint))
            {
                options.Endpoint = openTelemetryConfig.Exporter.OtlpEndpoint;

                // 헤더 설정
                foreach (var header in openTelemetryConfig.Exporter.OtlpHeaders)
                {
                    options.Headers.Add(header.Key, header.Value);
                }

                // 프로토콜 설정
                options.Protocol = openTelemetryConfig.Exporter.OtlpProtocol.ToLowerInvariant() switch
                {
                    "http/protobuf" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf,
                    "grpc" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc,
                    _ => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc
                };
            }

            // 리소스 속성 추가
            options.ResourceAttributes.Add("service.name", openTelemetryConfig.ServiceName);
            options.ResourceAttributes.Add("service.version", openTelemetryConfig.ServiceVersion);
            options.ResourceAttributes.Add("deployment.environment", openTelemetryConfig.Environment);
        }
    }), bufferSize: 10000, blockWhenFull: false)
    .CreateLogger();

try
{
    // Serilog를 ASP.NET Core 로깅 시스템에 통합
    builder.Host.UseSerilog();

    // OpenTelemetry 서비스 등록
    builder.Services.AddOpenTelemetryServices(builder.Configuration, openTelemetryConfig.ServiceInstanceId, environment);
    
    builder.Services.AddFastEndpoints();
    builder.Services.AddOpenApi();

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();

    builder.Services.AddLiteBusApplication();
    builder.Services.AddDemoWebInfra(builder.Configuration);

    // RateLimit 설정을 DI 컨테이너에 등록
    builder.Services.Configure<RateLimitConfig>(builder.Configuration.GetSection(RateLimitConfig.SectionName));
    builder.Services.AddSingleton(rateLimitConfig);

    var app = builder.Build();
    
    // Rate Limit 미들웨어 등록 (FastEndpoints보다 먼저 등록)
    app.UseMiddleware<RateLimitMiddleware>();
    
    app.UseFastEndpoints(x =>
    {
        x.Errors.UseProblemDetails();
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Demo.Web API")
                .WithTheme(ScalarTheme.None)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.RestSharp)
                .WithCdnUrl("https://cdn.jsdelivr.net/npm/@scalar/api-reference@latest/dist/browser/standalone.js");
        });
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// 테스트에서 접근할 수 있도록 Program 클래스를 public으로 선언
public partial class Program { }
