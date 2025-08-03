using Demo.Application.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infra;

public static class InfraInitialize
{
    public static IServiceCollection AddInfra(this IServiceCollection service, ConfigurationManager configuration)
    {
        var postgresConfig = configuration.GetSection("Postgres").Get<PostgresConfig>();
        if (postgresConfig is null)
            throw new NullReferenceException();

        service.AddSingleton(postgresConfig);
        service.AddTransient<IUserRepository, UserRepositoryPostgre>();
        return service;
    }
}
