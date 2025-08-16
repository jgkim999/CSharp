using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Infra.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infra;

public static class GamePulseInfraInitialize
{
    public static IServiceCollection AddGamePulse(this IServiceCollection services)
    {
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IIpToNationRepository, IpToNationRepository>();
        services.AddSingleton<IIpToNationCache, IpToNationRedisCache>();
        services.AddSingleton<IIpToNationService, IpToNationService>();
        services.AddTransient<IJwtRepository, RedisJwtRepository>();
        return services;
    }
}
