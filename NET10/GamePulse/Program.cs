using FastEndpoints;
using FastEndpoints.Security;

using Demo.Application.Configs;
using RedisConfig = Demo.Application.Configs.RedisConfig;
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
    var openTelemetryConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
    if (openTelemetryConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<OtelConfig>(builder.Configuration.GetSection("OpenTelemetry"));

    var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
    if (jwtConfig == null)
        throw new NullReferenceException();
    builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));

    var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    Log.Information("Starting application");

    builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
    builder.Services.AddAuthorization();

    builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);

    builder.Services.AddFastEndpoints();

    builder.Services.AddOpenApiServices();

    builder.Services.AddGamePulse();
    builder.Services.AddSodServices();
    builder.Services.AddSodInfrastructure();

    var openTelemetryBuilder = builder.Services.AddOpenTelemetryApplication(openTelemetryConfig);
    openTelemetryBuilder.AddOpenTelemetryInfrastructure(openTelemetryConfig);

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseFastEndpointsInitialize();
    //app.UseStaticFiles();

    // Prometheus 메트릭 엔드포인트 추가 (프로덕션 환경에서만)
    if (openTelemetryConfig.EnablePrometheusExporter)
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
