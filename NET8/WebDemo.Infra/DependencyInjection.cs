using Microsoft.Extensions.DependencyInjection;
using WebDemo.Application.Repositories;

namespace WebDemo.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraServices(this IServiceCollection services)
    {
        services.AddTransient<IWeatherForecastRepository, WeatherForecastRepository>();
        return services;
    }
}
