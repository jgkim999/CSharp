using Demo.Application;
using FastEndpoints;
using Demo.Application.Extensions;
using Demo.Infra;
using Demo.Infra.Extensions;
using Scalar.AspNetCore;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 환경별 설정 파일 추가
var environment = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(); // 환경 변수가 JSON 설정을 오버라이드

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    builder.AddSerilogApplication();

    Log.Information("Starting application");

    builder.AddGamePulseApplication();

    builder.Services.AddFastEndpoints();

    builder.Services.AddOpenApiServices();

    builder.Services.AddLiteBusApplication();

    builder.Services.AddGamePulseInfra();

    var builderResult = builder.AddOpenTelemetryApplication();
    builderResult.openTelemetryBuilder.AddOpenTelemetryInfrastructure(builderResult.otelConfig);

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseFastEndpointsInitialize();
    //app.UseStaticFiles();

    // Prometheus 메트릭 엔드포인트 추가 (프로덕션 환경에서만)
    if (builderResult.otelConfig.EnablePrometheusExporter)
    {
        app.MapPrometheusScrapingEndpoint();
    }

    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
        string[] versions = ["v1", "v2"];
        app.MapScalarApiReference(options => options.AddDocuments(versions));
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
