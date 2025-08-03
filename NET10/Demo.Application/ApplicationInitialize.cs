using Demo.Application.DTO;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application;

public static class ApplicationInitialize
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        service.AddMapster();
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MapsterConfig).Assembly);
        return service;
    }
}
