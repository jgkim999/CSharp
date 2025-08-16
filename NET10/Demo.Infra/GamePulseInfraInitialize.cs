using Demo.Application.Repositories;
using Demo.Application.Services;
using Demo.Application.Services.Auth;
using Demo.Infra.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infra;

public static class GamePulseInfraInitialize
{
    /// <summary>
    /// Registers GamePulse infrastructure services into the provided <see cref="IServiceCollection"/>:
    /// authentication service, IP-to-nation repository/cache/service, and JWT repository.
    /// </summary>
    /// <returns>The same <see cref="IServiceCollection"/> instance to allow fluent chaining.</returns>
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
