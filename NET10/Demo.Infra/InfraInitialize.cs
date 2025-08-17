using Demo.Application.Configs;
using Demo.Application.Middleware;
using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Application.Services.Sod;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services.Sod;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Demo.Infra;

public static class InfraInitialize
{
    public static IServiceCollection AddGamePulseInfra(this IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IIpToNationRepository, IpToNationRepository>();
        services.AddSingleton<IIpToNationCache, IpToNationRedisCache>();
        services.AddSingleton<IIpToNationService, IpToNationService>();
        services.AddTransient<IJwtRepository, RedisJwtRepository>();
        
        // ITelemetryService 및 TelemetryService를 Singleton으로 등록
        services.AddSingleton<ITelemetryService>(serviceProvider =>
        {
            var otelConfig = serviceProvider.GetRequiredService<IOptions<OtelConfig>>().Value;
            var logger = serviceProvider.GetRequiredService<ILogger<TelemetryService>>();

            return new TelemetryService(
                serviceName: otelConfig.ServiceName,
                serviceVersion: otelConfig.ServiceVersion,
                logger: logger
            );
        });
        
        services.AddSingleton<ISodBackgroundTaskQueue, SodBackgroundTaskQueue>();
        services.AddHostedService<SodBackgroundWorker>();

        return services;
    }
    
    public static IServiceCollection AddDemoWebInfra(this IServiceCollection services, ConfigurationManager configuration)
    {
        var postgresConfig = configuration.GetSection("Postgres").Get<PostgresConfig>();
        if (postgresConfig is null)
            throw new NullReferenceException();
        services.Configure<PostgresConfig>(configuration.GetSection("Postgres"));
        
        services.AddTransient<IJwtRepository, RedisJwtRepository>();
        services.AddTransient<IUserRepository, UserRepositoryPostgre>();
        return services;
    }
    
    public static WebApplication UseDemoWebInfra(this WebApplication app)
    {
        // Rate Limit 미들웨어 등록 (FastEndpoints보다 먼저 등록)
        app.UseMiddleware<RateLimitMiddleware>();
        return app;
    }
}
