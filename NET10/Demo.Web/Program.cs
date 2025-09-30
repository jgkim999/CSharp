using Demo.Application.Configs;
using Demo.Application.DTO.User;
using Demo.Application.Extensions;
using Demo.Application.Middleware;
using Demo.Application.Models;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Demo.Infra.Extensions;
using Demo.Web;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

using FluentValidation;
using Mapster;
using Scalar.AspNetCore;
using Serilog;

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
    builder.Services.AddSingleton<RabbitMqConnection>();
    builder.Services.AddSingleton<RabbitMqHandler>();
    builder.Services.AddSingleton<IMqMessageHandler, MqMessageHandler>();
    builder.Services.AddSingleton<IMqPublishService, RabbitMqPublishService>();

    #endregion

    // RateLimit 설정을 DI 컨테이너에 등록
    builder.Services.Configure<RateLimitConfig>(builder.Configuration.GetSection("RateLimit"));
        
    var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("Redis"));
    
    builder.Services.AddFastEndpoints();
    builder.Services.SwaggerDocument();
    //builder.Services.AddOpenApi();

    #region GraphQL
    // HotChocolate 서비스 등록
    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Demo.Web.GraphQL.QueryTypes.UserQueries>()
        .AddMutationType<Demo.Web.GraphQL.MutationTypes.UserMutations>()
        .AddAuthorization();        // 보안을 위해 필수
    #endregion
    
    // CORS 설정 추가
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ReactApp", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // 개발 환경에서는 모든 origin 허용 (보안상 주의)
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            }
            else
            {
                // 프로덕션 환경에서는 특정 origin만 허용
                policy.WithOrigins("http://localhost:3000", "http://127.0.0.1:3000", "http://192.168.0.60:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });

    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();

    builder.AddLiteBusApplication();
    
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
    
    // DbContextFactory 등록
    builder.Services.AddDbContextFactory<DemoDbContext>(options =>
        options.UseNpgsql(postgresConfig.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(10); // 명령 타임아웃 10초로 제한
        }));
    
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();
    builder.Services.AddTransient<IUserRepository, UserRepositoryPostgre>();
    builder.Services.AddTransient<ICompanyRepository, CompanyRepositoryPostgre>();
    builder.Services.AddTransient<IProductRepository, ProductRepositoryPostgre>();
    
    // FusionCache 설정 추가
    builder.Services.AddIpToNationFusionCache(builder.Configuration);
    
    #endregion
    
    builder.Services.AddHostedService<RabbitMqConsumerService>();
    
    var app = builder.Build();
    
    // CORS 미들웨어 등록 (가장 먼저 등록)
    app.UseCors("ReactApp");
    
    // Rate Limit 미들웨어 등록 (FastEndpoints보다 먼저 등록)
    app.UseMiddleware<RateLimitMiddleware>();
    
    app.UseFastEndpointsInitialize();

    #region GraphQL
    // HotChocolate 미들웨어 매핑
    app.MapGraphQL("/graphql")
       .RequireCors("ReactApp"); // CORS 정책 적용
    #endregion
    
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

/// <summary>
/// 테스트에서 접근할 수 있도록 Program 클래스를 public으로 선언 
/// </summary>
public partial class Program { }
