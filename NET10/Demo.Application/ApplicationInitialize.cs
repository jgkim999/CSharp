using Demo.Application.Configs;
using Demo.Application.DTO.User;
using Demo.Application.Extensions;
using FastEndpoints.Security;
using LiteBus.Commands.Extensions.MicrosoftDependencyInjection;
using LiteBus.Events.Extensions.MicrosoftDependencyInjection;
using LiteBus.Messaging.Extensions.MicrosoftDependencyInjection;
using LiteBus.Queries.Extensions.MicrosoftDependencyInjection;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
        
        builder.Host.UseSerilog();
        
        return builder;
    }
    
    /// <summary>
    /// Configures application-level services, including LiteBus command handling and Mapster object mapping, and registers them with the dependency injection container.
    /// </summary>
    /// <summary>
    /// Configures LiteBus and Mapster application services on the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// - Registers LiteBus command, query, and event modules by scanning the assembly that contains <see cref="ApplicationInitialize"/>.
    /// - Adds Mapster and scans the assembly that contains <see cref="MapsterConfig"/> to register mappings into the global <see cref="TypeAdapterConfig"/>.
    /// </remarks>
    /// <returns>The updated <see cref="IServiceCollection"/> with application services registered.</returns>
    public static IServiceCollection AddLiteBusApplication(this IServiceCollection service)
    {
        service.AddLiteBus(liteBus =>
        {
            var applicationAssembly = typeof(ApplicationInitialize).Assembly;
            
            liteBus.AddCommandModule(module =>
            {
                module.RegisterFromAssembly(applicationAssembly);
            });
            liteBus.AddQueryModule(module =>
            {
                module.RegisterFromAssembly(applicationAssembly);
            });
            liteBus.AddEventModule(module =>
            {
                module.RegisterFromAssembly(applicationAssembly);
            });
        });
        
        service.AddMapster();
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(typeof(MapsterConfig).Assembly);
        return service;
    }

    public static WebApplicationBuilder AddDemoWebApplication(this WebApplicationBuilder builder)
    {
        // RateLimit 설정을 DI 컨테이너에 등록
        builder.Services.Configure<RateLimitConfig>(builder.Configuration.GetSection("RateLimit"));
        
        var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
        if (redisConfig is null)
            throw new NullReferenceException();
        builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));
        
        return builder;
    }
    
    public static WebApplicationBuilder AddGamePulseApplication(this WebApplicationBuilder builder)
    {
        var jwtConfig = builder.Configuration.GetSection("Jwt").Get<JwtConfig>();
        if (jwtConfig == null)
            throw new NullReferenceException();
        builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("Jwt"));

        var redisConfig = builder.Configuration.GetSection("RedisConfig").Get<RedisConfig>();
        if (redisConfig is null)
            throw new NullReferenceException();
        builder.Services.Configure<RedisConfig>(builder.Configuration.GetSection("RedisConfig"));
        
        builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
        builder.Services.AddAuthorization();
        
        builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);
        return builder;
    }
}
