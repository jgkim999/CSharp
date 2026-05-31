using Demo.Application.WeatherForecast;
using LiteBus.Commands;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddLiteBusApplication(this IServiceCollection services)
    {
        // Register LiteBus with all modules
        services.AddLiteBus(liteBus =>
        {
            liteBus.AddCommandModule(module => module.Register(typeof(WeatherForecastHandler)));
        });

        return services;
    }
}
