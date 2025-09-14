using System.Globalization;
using System.Reflection;
using Blazored.LocalStorage;
using Demo.Admin;
using MudBlazor.Services;
using RestSharp;
using Demo.Admin.Components;
using Demo.Application.Configs;
using Demo.Application.Services;
using Demo.Domain;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Demo.Admin.Services;
using OpenTelemetryBuilder = OpenTelemetry.OpenTelemetryBuilder;

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
    #region SeriLog
    builder.Host.UseSerilog();
    builder.Services.AddSerilog((services, lc) =>
    {
        lc.ReadFrom.Configuration(builder.Configuration);
        lc.ReadFrom.Services(services);
    });
    #endregion
    
    Log.Information("Starting application");
    
    builder.AddOpenTelemetryApplication(Log.Logger);
    
    #region RabbitMQ
    var rabbitMqConfig = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqConfig>();
    if (rabbitMqConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
    builder.Services.AddSingleton<IMqPublishService, RabbitMqPublishService>();
    #endregion

    #region Redis
    var redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
    if (redisConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("Redis"));
    #endregion
    
    #region PgSql
    var postgresConfig = builder.Configuration.GetSection("Postgres").Get<PostgresConfig>();
    if (postgresConfig is null)
        throw new NullReferenceException();
    builder.Services.Configure<PostgresConfig>(builder.Configuration.GetSection("Postgres"));
    builder.Services.AddDbContextFactory<DemoDbContext>(options =>
        options.UseNpgsql(postgresConfig.ConnectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(10); // 명령 타임아웃 10초로 제한
        }));
    #endregion
    
    builder.Services.AddBlazoredLocalStorage();
    // Add MudBlazor services
    builder.Services.AddMudServices();

    // HttpClient 설정 (Demo.Web API 호출용) - 연결 풀링 최적화
    builder.Services.AddHttpClient("DemoWebApi", client =>
    {
        client.BaseAddress = new Uri("http://localhost:5266/");
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "Demo.Admin/1.0");
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
    {
        MaxConnectionsPerServer = 10, // 서버당 최대 연결 수 제한
        UseCookies = false // 쿠키 비활성화로 성능 향상
    });

    // RestSharp 클라이언트 설정 - HttpClient 풀링 활용
    builder.Services.AddSingleton<RestClient>(provider =>
    {
        var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("DemoWebApi");
        
        var restClientOptions = new RestClientOptions()
        {
            BaseUrl = new Uri("http://localhost:5266/"),
            ConfigureMessageHandler = _ => new HttpClientHandler()
            {
                MaxConnectionsPerServer = 10,
                UseCookies = false
            }
        };
        
        return new RestClient(httpClient, restClientOptions);
    });

    // Demo.Admin 서비스 등록
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ICompanyService, CompanyService>();
    builder.Services.AddScoped<IProductService, ProductService>();

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    
    // Graceful shutdown 설정
    builder.Services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(10); // 기본 30초에서 10초로 단축
    });
    
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    app.UseAntiforgery();

    app.MapStaticAssets();
    
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.MapDefaultEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

