using Demo.Application.Commands;
using Demo.Application.DTO.User;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application;

public static class ApplicationInitialize
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        service.AddLiteBus(liteBus =>
        {
            liteBus.AddCommandModule(module =>
            {
                module.RegisterFromAssembly(typeof(UserCreateCommand).Assembly);
            });
        });

        service.AddMapster();
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MapsterConfig).Assembly);
        return service;
    }
}
