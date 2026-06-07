using Dapper;
using Demo.Application;
using Demo.Infra;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Microsoft.Extensions.Caching.Redis;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using WebApiService.PrePostProcessor;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Services.AddSerilog();

    // 로깅 레벨 설정
    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog();
    if (builder.Environment.IsDevelopment())
    {
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
    }

    builder.Services.AddLiteBusApplication();

    // JWT 설정 정보 가져오기
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var key = jwtSettings["Key"]!;

    // FastEndpoints 설정
    builder.Services
        .AddAuthenticationJwtBearer(s => s.SigningKey = key) // JWT
        .AddAuthorization() // Authorization
        .AddFastEndpoints()
        .SwaggerDocument();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    
    // Valkey 연결 (문자열로 접속)
    // 개발 환경: Aspire가 자동으로 주입
    // 프로덕션 환경: appsettings.json에서 가져옴
    var valkeyConnectionString = builder.Configuration.GetConnectionString("redis");

    if (builder.Environment.IsDevelopment())
    {
        Log.Information($"[Development] Valkey: {valkeyConnectionString}");
    }
    else
    {
        Log.Information($"[Production] Valkey: {valkeyConnectionString}");
    }

    builder.Services.AddFusionCache()
        .WithSerializer(new FusionCacheSystemTextJsonSerializer())
        .WithDistributedCache(new RedisCache(new RedisCacheOptions { Configuration = valkeyConnectionString }))
        .WithBackplane(new RedisBackplane(new RedisBackplaneOptions { Configuration = valkeyConnectionString }))
        .AsHybridCache();

    // Resolve MySQL connection string injected by Aspire: try 'mydb' then 'mysql'
    var mysqlConnectionString = builder.Configuration.GetConnectionString("mydb") ?? builder.Configuration.GetConnectionString("mysql");
    if (string.IsNullOrEmpty(mysqlConnectionString))
    {
        Log.Warning("MySQL connection string not found in Configuration. Ensure Aspire injected 'mydb' or 'mysql' connection string.");
    }

    // Dapper 전역 설정 추가 (앱 시작 시 최초 1회 실행)
    DefaultTypeMap.MatchNamesWithUnderscores = true;
    
    builder.Services.AddSingleton<IDbManager>(sp => new MySqlManager(mysqlConnectionString));
    builder.Services.AddSingleton<IUserService, UserService>();
    
    var app = builder.Build();

    app.MapDefaultEndpoints();

    // FastEndpoints 먼저 등록
    app.UseDefaultExceptionHandler()
        .UseAuthentication() // authentication 먼저 등록
        .UseAuthorization() // authorization 다음 등록
        .UseFastEndpoints(c =>
        {
            c.Endpoints.Configurator = ep =>
            {
                ep.PreProcessors(Order.Before, typeof(RequestLogger<>));
                //ep.PostProcessors(Order.After, typeof(ResponseLogger));
            };
        });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
        app.MapScalarApiReference(options =>
        {
            options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.RestSharp);
        });
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
