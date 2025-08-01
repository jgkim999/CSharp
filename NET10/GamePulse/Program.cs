using FastEndpoints;
using FastEndpoints.Security;

using GamePulse;
using GamePulse.Configs;
using GamePulse.Repositories;
using GamePulse.Repositories.IpToNation;
using GamePulse.Repositories.Jwt;
using GamePulse.Services.Auth;
using GamePulse.Services.IpToNation;
using GamePulse.Sod;

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
    
    builder.Services.AddSingleton<IAuthService, AuthService>();
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();
    builder.Services.AddSingleton<IIpToNationRepository, IpToNationRepository>();
    builder.Services.AddSingleton<IIpToNationCache, IpToNationCache>();
    builder.Services.AddSingleton<IIpToNationService, IpToNationService>();
    
    builder.Services.AddSod();
    
    builder.Services.AddOpenTelemetryServices(openTelemetryConfig);

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthentication();
    app.UseFastEndpointsInitialize();
    //app.UseStaticFiles();
    
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
