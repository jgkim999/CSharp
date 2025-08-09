using Demo.Application.Commands;
using Demo.Application.DTO.User;
using Demo.Application.Extensions;
using Demo.Application.Services;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application;

public static class ApplicationInitialize
{
    /// <summary>
    /// Configures application-level services, including LiteBus command handling and Mapster object mapping, and registers them with the dependency injection container.
    /// </summary>
    /// <returns>The updated <see cref="IServiceCollection"/> with application services registered.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        // TelemetryService 등록
        service.AddTelemetryService();

        service.AddLiteBus(liteBus =>
        {
            liteBus.AddCommandModule(module =>
            {
                module.RegisterFromAssembly(typeof(UserCreateCommand).Assembly);
            });
        });

        // LiteBus 텔레메트리 데코레이터 추가
        service.AddLiteBusTelemetry();

        service.AddMapster();
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MapsterConfig).Assembly);
        return service;
    }
}
