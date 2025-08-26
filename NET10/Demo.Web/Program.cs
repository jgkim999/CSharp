using Demo.Application;
using Demo.Application.Extensions;
using Demo.Domain;
using Demo.Infra;
using Demo.Infra.Configs;
using Demo.Infra.Extensions;
using Demo.Infra.Services;
using Demo.Web.Endpoints.User;
using FastEndpoints;

using FluentValidation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;
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

    {
        var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
        if (rabbitMqConfig is null)
            throw new NullReferenceException();
        builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
        builder.Services.AddSingleton<IMqPublishService, RabbitMqPublishService>();
    }
    
    builder.AddDemoWebApplication();
    
    builder.Services.AddFastEndpoints();
    
    //builder.Services.AddOpenApiServices();
    builder.Services.AddOpenApi();

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();

    builder.Services.AddLiteBusApplication();
    builder.Services.AddDemoWebInfra(builder.Configuration);

    builder.Services.AddHostedService<RabbitMqConsumerService>();
    
    var app = builder.Build();
    
    app.UseDemoWebInfra();
    
    app.UseFastEndpointsInitialize();

    app.UseOpenApi(options =>
    {
        options.Path = "/openapi/{documentName}.json";
    });
    app.MapOpenApi(); //.CacheOutput();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle(app.Environment.ApplicationName)
            .WithTheme(ScalarTheme.None)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.RestSharp)
            .WithCdnUrl("https://cdn.jsdelivr.net/npm/@scalar/api-reference@latest/dist/browser/standalone.js");
    });
    
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
