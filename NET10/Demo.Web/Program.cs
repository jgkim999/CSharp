using Demo.Application;
using Demo.Application.Configs;
using Demo.Application.DTO.User;
using LiteBus.Queries.Abstractions;
using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using Demo.Application.ErrorHandlers;
using Demo.Application.Extensions;
using Demo.Application.Middleware;
using Demo.Domain;
using Demo.Domain.Repositories;
using Demo.Infra;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Demo.Web;
using Demo.Web.Endpoints.User;
using FastEndpoints;

using FluentValidation;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Events.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using LiteBus.Queries.Extensions.MicrosoftDependencyInjection;
using Mapster;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Sinks.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Aspire ServiceDefaults 추가
builder.AddServiceDefaults();

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
    builder.AddOpenTelemetryApplication(Log.Logger);

    #region RabbitMQ

    var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
    if (rabbitMqConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMqPublishService, RabbitMqPublishService>();

    #endregion

    // RateLimit 설정을 DI 컨테이너에 등록
    builder.Services.Configure<RateLimitConfig>(builder.Configuration.GetSection("RateLimit"));
        
    var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));
    
    builder.Services.AddFastEndpoints();
    
    builder.Services.AddOpenApi();

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();

    #region LiteBus
    // Handler 등록
    builder.Services.AddTransient<IQueryErrorHandler, QueryErrorHandler>();
    builder.Services.AddTransient<IQueryPreHandler, QueryPreHandler>();
    builder.Services.AddTransient<IQueryPostHandler, QueryPostHandler>();
    
    builder.Services.AddTransient<ICommandErrorHandler, CommandErrorHandler>();
    builder.Services.AddTransient<ICommandPreHandler, CommandPreHandler>();
    builder.Services.AddTransient<ICommandPostHandler, CommandPostHandler>();
    
    builder.Services.AddTransient<IEventErrorHandler, EventErrorHandler>();
    builder.Services.AddTransient<IEventPreHandler, EventPreHandler>();
    builder.Services.AddTransient<IEventPostHandler, EventPostHandler>();
    
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
    #endregion
    
    #region Mapster
    builder.Services.AddMapster();
    var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
    typeAdapterConfig.Scan(typeof(MapsterConfig).Assembly);
    #endregion

    #region Infra
    var postgresConfig = builder.Configuration.GetSection("Postgres").Get<PostgresConfig>();
    if (postgresConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<PostgresConfig>(builder.Configuration.GetSection("Postgres"));
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();
    builder.Services.AddTransient<IUserRepository, UserRepositoryPostgre>();
    
    #endregion
    
    builder.Services.AddHostedService<RabbitMqConsumerService>();
    
    var app = builder.Build();
    
    // Rate Limit 미들웨어 등록 (FastEndpoints보다 먼저 등록)
    app.UseMiddleware<RateLimitMiddleware>();
    
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
    
    app.MapDefaultEndpoints();
    
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
