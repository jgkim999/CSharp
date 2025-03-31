using Demo.Application.Repositories;
using Demo.Infra.Config;
using Demo.Infra.Repositories;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

namespace Demo.Infra;

public static class InfraInitialize
{
    public static WebApplicationBuilder AddInfraServices(this WebApplicationBuilder builder)
    {
        RedisConfig? redisConfig = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
        if (redisConfig is null)
        {
            throw new NullReferenceException("Redis configuration is missing");
        }

        builder.Services.Configure<MySqlConfig>(builder.Configuration.GetSection("MySql"));
        builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("Redis"));
        builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));

        // Add Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig?.ConnectionString));

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConfig?.ConnectionString;
            options.ConnectionMultiplexerFactory = async () => await ConnectionMultiplexer.ConnectAsync(redisConfig?.ConnectionString);
        });

        // Add HybridCache - it will automatically use Redis as L2
        /*
#pragma warning disable EXTEXP0018 // 형식은 평가 목적으로 제공되며, 이후 업데이트에서 변경되거나 제거될 수 있습니다. 계속하려면 이 진단을 표시하지 않습니다.
        builder.Services.AddHybridCache(options =>
        {
            // Maximum size of cached items
            options.MaximumPayloadBytes = 1024 * 1024 * 10; // 10MB
            options.MaximumKeyLength = 512;

            // Default timeouts
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(2),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };
        });
#pragma warning restore EXTEXP0018 // 형식은 평가 목적으로 제공되며, 이후 업데이트에서 변경되거나 제거될 수 있습니다. 계속하려면 이 진단을 표시하지 않습니다.
        */

        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IEmployeeCacheRepository, EmployeeCacheRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();

        return builder;
    }
}
