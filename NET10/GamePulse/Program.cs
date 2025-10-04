using Demo.Application.Configs;
using Demo.Application.DTO.User;
using FastEndpoints;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Application.Services.Sod;
using Demo.Domain.Repositories;

using Demo.Infra.Repositories;
using Demo.Infra.Services.Sod;
using FastEndpoints.Security;
using GamePulse;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Events.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using LiteBus.Queries.Extensions.MicrosoftDependencyInjection;
using Mapster;
using Microsoft.Extensions.Options;
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
    #region SeriLog
    builder.Host.UseSerilog();

    builder.Services.AddSerilog((services, lc) =>
    {
        lc.ReadFrom.Configuration(builder.Configuration);
        lc.ReadFrom.Services(services);
        lc.Enrich.FromLogContext();
    });
    #endregion

    Log.Information("Starting application");

    #region Redis
    var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));
    #endregion

    #region  Jwt
    //appBuilder.AddGamePulseApplication();
    var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
    if (jwtConfig == null)
        throw new NullReferenceException();
    builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));
    builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
    builder.Services.AddAuthorization();
    builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);
    #endregion

    #region AddGamePulseInfra
    //appBuilder.Services.AddGamePulseInfra();
    builder.Services.AddSingleton<IAuthService, AuthService>();
    builder.Services.AddSingleton<IIpToNationRepository, IpToNationRepository>();
    builder.Services.AddSingleton<IIpToNationCache, IpToNationRedisCache>();
    builder.Services.AddSingleton<IIpToNationService, IpToNationService>();
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();
    builder.Services.AddTransient<IUserRepository, UserRepositoryPostgre>();

    // ITelemetryService 및 TelemetryService를 Singleton으로 등록
    builder.Services.AddSingleton<ITelemetryService>(serviceProvider =>
    {
        var openTelemetryConfig = serviceProvider.GetRequiredService<IOptions<OtelConfig>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<TelemetryService>>();

        return new TelemetryService(
            serviceName: openTelemetryConfig.ServiceName,
            serviceVersion: openTelemetryConfig.ServiceVersion,
            logger: logger
        );
    });

    builder.Services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
    builder.Services.AddHostedService<SodBackgroundWorker>();
    #endregion

    #region LiteBus
    // 모든 로드된 Assembly 가져오기
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

    builder.Services.AddLiteBus(liteBus =>
    {
        liteBus.AddCommandModule(module =>
        {
            foreach (var assembly in assemblies)
            {
                module.RegisterFromAssembly(assembly);
            }
        });

        liteBus.AddQueryModule(module =>
        {
            foreach (var assembly in assemblies)
            {
                module.RegisterFromAssembly(assembly);
            }
        });

        liteBus.AddEventModule(module =>
        {
            foreach (var assembly in assemblies)
            {
                module.RegisterFromAssembly(assembly);
            }
        });
    });

    #region Mapster
    builder.Services.AddMapster();
    var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
    typeAdapterConfig.Scan(typeof(MapsterConfig).Assembly);
    #endregion

    #endregion

    builder.Services.AddOpenApi();

    #region AddFastEndpoints
    builder.Services.AddFastEndpoints().AddOpenApiDocument();
    #endregion
    //appBuilder.Services.AddOpenApiServices();

    #region OpenTelemetry
    builder.AddOpenTelemetryApplication(Log.Logger);
    #endregion

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();

    #region UseFastEndpoint
    app.UseDefaultExceptionHandler();
    app.UseFastEndpoints(c =>
    {
        c.Endpoints.ShortNames = true;
        c.Versioning.Prefix = "v";
        c.Errors.UseProblemDetails();
    });

    //if (app.Environment.IsDevelopment())
    //{
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
    //}
    #endregion

    // Install the following NuGet packages:
    // dotnet add package Microsoft.AspNetCore.Http.Abstractions
    // dotnet add package Microsoft.AspNetCore.Routing
    //app.MapDefaultEndpoints();

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
