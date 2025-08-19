using Demo.Application;
using Demo.Application.Configs;
using Demo.Application.DTO.User;
using FastEndpoints;
using Demo.Application.Extensions;
using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Application.Services.Sod;

using Demo.Infra.Extensions;
using Demo.Infra.Repositories;
using Demo.Infra.Services.Sod;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
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
    builder.AddSerilogApplication();

    Log.Information("Starting application");

    #region  AddGamePulseApplication
    //builder.AddGamePulseApplication();
    var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
    if (jwtConfig == null)
        throw new NullReferenceException();
    builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));

    var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));

    builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
    builder.Services.AddAuthorization();

    builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);
    #endregion

    #region AddGamePulseInfra
    //builder.Services.AddGamePulseInfra();
    builder.Services.AddSingleton<IAuthService, AuthService>();
    builder.Services.AddSingleton<IIpToNationRepository, IpToNationRepository>();
    builder.Services.AddSingleton<IIpToNationCache, IpToNationRedisCache>();
    builder.Services.AddSingleton<IIpToNationService, IpToNationService>();
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();
    builder.Services.AddTransient<IUserRepository, UserRepositoryPostgre>();

    // ITelemetryService 및 TelemetryService를 Singleton으로 등록
    builder.Services.AddSingleton<ITelemetryService>(serviceProvider =>
    {
        var otelConfig = serviceProvider.GetRequiredService<IOptions<OtelConfig>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<TelemetryService>>();

        return new TelemetryService(
            serviceName: otelConfig.ServiceName,
            serviceVersion: otelConfig.ServiceVersion,
            logger: logger
        );
    });

    builder.Services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
    builder.Services.AddHostedService<SodBackgroundWorker>();
    #endregion

    #region AddLiteBusApplication
    builder.Services.AddLiteBusApplication();
    builder.Services.AddLiteBus(liteBus =>
    {
        var applicationAssembly = typeof(ApplicationInitialize).Assembly;

        liteBus.AddCommandModule(module =>
        {
            module.RegisterFromAssembly(applicationAssembly);
        });
        liteBus.AddQueryModule(module =>
        {
            module.RegisterFromAssembly(applicationAssembly);
        });
        liteBus.AddEventModule(module =>
        {
            module.RegisterFromAssembly(applicationAssembly);
        });
    });

    // LiteBus 텔레메트리 데코레이터 추가
    builder.Services.AddLiteBusTelemetry();

    builder.Services.AddMapster();
    var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
    typeAdapterConfig.Scan(typeof(MapsterConfig).Assembly);

    #endregion

    builder.Services.AddOpenApi();

    #region AddFastEndpoints
    builder.Services.AddFastEndpoints().AddOpenApiDocument();
    #endregion
    //builder.Services.AddOpenApiServices();

    #region Configure OpenAPI

/*
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, context, _) =>
        {
            document.Info = new()
            {
                Title = "Product Catalog API",
                Version = "v1",
                Description = """
                              Modern API for managing product catalogs.
                              Supports JSON and XML responses.
                              Rate limited to 1000 requests per hour.
                              """,
                Contact = new()
                {
                    Name = "API Support",
                    Email = "api@example.com",
                    Url = new Uri("https://api.example.com/support")
                }
            };
            return Task.CompletedTask;
        });
    });
*/
    #endregion

    #region OpenTelemetry
    var builderResult = builder.AddOpenTelemetryApplication(Log.Logger);
    builderResult.openTelemetryBuilder.AddOpenTelemetryInfrastructure(builderResult.otelConfig);
    #endregion

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();

    #region UseFastEndpoint
    //app.UseFastEndpointsInitialize();
    app.UseDefaultExceptionHandler();
    app.UseFastEndpoints(c =>
    {
        c.Endpoints.ShortNames = true;
        c.Versioning.Prefix = "v";
        c.Errors.UseProblemDetails();
        /*
        c.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
        {
            return new ValidationProblemDetails(failures.GroupBy(f => f.PropertyName)
                .ToDictionary(keySelector: e => e.Key,
                    elementSelector: e => e.Select(m => m.ErrorMessage).ToArray()))
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", ctx.TraceIdentifier } }
            };
        };
        */
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

    //app.UseStaticFiles();

    // Prometheus 메트릭 엔드포인트 추가 (프로덕션 환경에서만)
    if (builderResult.otelConfig.EnablePrometheusExporter)
    {
        //app.MapPrometheusScrapingEndpoint();
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
