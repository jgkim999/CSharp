using MassTransit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using WebDemo.Application.WeatherService;
using WebDemo.Domain.Configs;

namespace WebDemo.Application;

public static class InitializeMassTransIt
{
    public static IServiceCollection AddMassTransItProducer(this IServiceCollection services)
    {
        var rabbitMqConfig = services.GetConfiguration().GetSection("RabbitMq").Get<RabbitMqConfig>();
        if (rabbitMqConfig is null)
            throw new ArgumentNullException("RabbitMq config is null");

        services.AddSingleton(rabbitMqConfig);

        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMqConfig.Username);
                    h.Password(rabbitMqConfig.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
        return services;
    }

    public static IServiceCollection AddMassTransItConsumer(this IServiceCollection services)
    {
        var rabbitMqConfig = services.GetConfiguration().GetSection("RabbitMq").Get<RabbitMqConfig>();
        if (rabbitMqConfig is null)
            throw new ArgumentNullException("RabbitMq config is null");

        services.AddSingleton(rabbitMqConfig);

        services.AddMassTransit(x =>
        {
            //x.AddConsumers(entryAssembly);
            x.AddConsumer<WeatherRabbitMqConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMqConfig.Username);
                    h.Password(rabbitMqConfig.Password);
                });
                cfg.ConfigureEndpoints(context);
            });
        });
        return services;
    }
}