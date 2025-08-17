using Demo.Application.Configs;
using Demo.Application.Extensions;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Demo.Application;

public static class GamePulseInitialize
{
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
