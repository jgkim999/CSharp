using Demo.Application;
using Demo.Application.Extensions;
using Demo.Infra;
using Demo.Infra.Extensions;
using Demo.Web.Endpoints.User;
using FastEndpoints;

using FluentValidation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

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

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    //builder.AddSerilogApplication();
    builder.Host.UseSerilog();
    
    builder.Services.AddSerilog((services, lc) =>
    {
        lc.ReadFrom.Configuration(builder.Configuration);
        lc.ReadFrom.Services(services);
    });
    
    Log.Information("Starting application");

    // OpenTelemetry 서비스 등록
    var openTelemetry = builder.AddOpenTelemetryApplication(Log.Logger);
    openTelemetry.openTelemetryBuilder.AddOpenTelemetryInfrastructure(openTelemetry.otelConfig);
    
    builder.AddDemoWebApplication();
    
    builder.Services.AddFastEndpoints();
    
    builder.Services.AddOpenApiServices();

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();

    builder.Services.AddLiteBusApplication();
    builder.Services.AddDemoWebInfra(builder.Configuration);
    
    var app = builder.Build();
    
    app.UseDemoWebInfra();
    
    app.UseFastEndpointsInitialize();

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
