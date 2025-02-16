using Demo.Application.Repositories;
using Demo.Infra.Config;
using Demo.Infra.Repositories;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Infra;

public static class InfraInitialize
{
    public static WebApplicationBuilder AddInfraServices(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<MySqlConfig>(builder.Configuration.GetSection("MySql"));
        builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("Redis"));
        builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));

        builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();

        return builder;
    }
}
