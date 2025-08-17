using Demo.Application.DTO.User;
using Demo.Application.Extensions;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Events.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using LiteBus.Queries.Extensions.MicrosoftDependencyInjection;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Demo.Application;

public static class ApplicationInitialize
{
    public static WebApplicationBuilder AddSerilogApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());
        return builder;
    }
    
    /// <summary>
    /// Configures application-level services, including LiteBus command handling and Mapster object mapping, and registers them with the dependency injection container.
    /// </summary>
    /// <returns>The updated <see cref="IServiceCollection"/> with application services registered.</returns>
    public static IServiceCollection AddLiteBusApplication(this IServiceCollection service)
    {
        service.AddLiteBus(liteBus =>
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                liteBus.AddCommandModule(module =>
                {
                    module.RegisterFromAssembly(assembly);
                    //module.RegisterFromAssembly(typeof(AppDomain).Assembly);
                });
                liteBus.AddQueryModule(module =>
                {
                    module.RegisterFromAssembly(assembly);
                });
                liteBus.AddEventModule(module =>
                {
                    module.RegisterFromAssembly(assembly);
                });
            }
        });

        // LiteBus 텔레메트리 데코레이터 추가
        service.AddLiteBusTelemetry();

        service.AddMapster();
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MapsterConfig).Assembly);
        return service;
    }
}
