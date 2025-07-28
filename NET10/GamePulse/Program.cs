using FastEndpoints;
using FastEndpoints.Security;
using GamePulse;
using GamePulse.Configs;
using GamePulse.Repositories.Jwt;
using GamePulse.Services;
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
    var otelConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
    if (otelConfig is null)
        throw new NullReferenceException();
    
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());
    
    Log.Information("Starting application");
    
    var section = builder.Configuration.GetSection("Jwt");
    var jwtConfig = section.Get<JwtConfig>();
    if (jwtConfig == null)
    {
        throw new NullReferenceException();
    }

    builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
    builder.Services.AddAuthorization();

    builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);

    builder.Services.AddFastEndpoints();

    builder.Services.AddOpenApiServices();
    
    builder.Services.AddSingleton<IAuthService, AuthService>();
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();

    builder.Services.AddOpenTelemetryServices(otelConfig);

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthentication();
    app.UseFastEndpointsInitialize();
    
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
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
