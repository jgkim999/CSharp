using Demo.Application;
using Demo.Infra;
using Demo.Web.Configs;
using Demo.Web.Endpoints.User;
using Demo.Web.Extensions;
using FastEndpoints;
using FluentValidation;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 환경별 설정 파일 추가
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

// OpenTelemetry 설정 바인딩
var otelConfig = new OpenTelemetryConfig();
builder.Configuration.GetSection(OpenTelemetryConfig.SectionName).Bind(otelConfig);

// Serilog와 OpenTelemetry 통합 설정
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", otelConfig.ServiceName)
    .Enrich.WithProperty("ServiceVersion", otelConfig.ServiceVersion)
    .Enrich.WithProperty("Environment", otelConfig.Environment)
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "{NewLine}{Exception} " +
        "TraceId={TraceId} SpanId={SpanId}")
    .WriteTo.OpenTelemetry(options =>
    {
        // OpenTelemetry 로깅이 활성화된 경우에만 설정
        if (otelConfig.Logging.Enabled)
        {
            // OTLP 엔드포인트가 설정된 경우 사용
            if (!string.IsNullOrEmpty(otelConfig.Exporter.OtlpEndpoint))
            {
                options.Endpoint = otelConfig.Exporter.OtlpEndpoint;
                
                // 헤더 설정
                foreach (var header in otelConfig.Exporter.OtlpHeaders)
                {
                    options.Headers.Add(header.Key, header.Value);
                }
                
                // 프로토콜 설정
                options.Protocol = otelConfig.Exporter.OtlpProtocol.ToLowerInvariant() switch
                {
                    "http/protobuf" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf,
                    "grpc" => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc,
                    _ => Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc
                };
            }
            
            // 리소스 속성 추가
            options.ResourceAttributes.Add("service.name", otelConfig.ServiceName);
            options.ResourceAttributes.Add("service.version", otelConfig.ServiceVersion);
            options.ResourceAttributes.Add("deployment.environment", otelConfig.Environment);
        }
    })
    .CreateLogger();

try
{
    // Serilog를 ASP.NET Core 로깅 시스템에 통합
    builder.Host.UseSerilog();

    builder.Services.AddFastEndpoints();
    builder.Services.AddOpenApi();

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();
    
    builder.Services.AddApplication();
    builder.Services.AddInfra(builder.Configuration);
    
    // OpenTelemetry 서비스 등록
    builder.Services.AddOpenTelemetryServices(builder.Configuration);

    var app = builder.Build();
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
                .WithTheme(ScalarTheme.BluePlanet)
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
