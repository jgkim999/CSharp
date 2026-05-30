using FastEndpoints;
using Microsoft.Extensions.Caching.Redis;
using Scalar.AspNetCore;
using Serilog;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry()
    .CreateLogger();
try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Services.AddSerilog();

    builder.Services.AddFastEndpoints();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi(options =>
    {
        // Scalar의 기본 코드 생성 언어를 C# RestSharp으로 설정
        // (Scalar 대시보드에서 표시되는 기본값)
        // Note: Scalar SDK 버전에 따라 이 설정이 다를 수 있습니다
    });
    
    // Valkey 연결 (문자열로 접속)
    // 개발 환경: Aspire가 자동으로 주입
    // 프로덕션 환경: appsettings.json에서 가져옴
    var valkeyConnectionString = builder.Configuration.GetConnectionString("valkey");

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

    var app = builder.Build();

    app.MapDefaultEndpoints();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.RestSharp);
        });
    }

    app.UseFastEndpoints();
    
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
