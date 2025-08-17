using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
