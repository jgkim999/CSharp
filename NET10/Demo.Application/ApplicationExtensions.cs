using Demo.Application.Account;
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
            // Register both handlers in a single command module to avoid duplicate module keys
            liteBus.AddCommandModule(module => {
                module.Register(typeof(WeatherForecastHandler));
                module.Register(typeof(AccountLoginHandler));
                module.Register(typeof(AccountCreateHandler));
            });
        });

        return services;
    }
}
