using JwtPractice.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace JwtPractice.Extensions;

public static class AddJWTTokenServicesExtensions
{
    public static IServiceCollection AddJWTTokenServices(this IServiceCollection services, ConfigurationManager configuration) {
        // Add Jwt Setings
        var bindJwtSettings = new JwtSettings();
        configuration.Bind("JsonWebTokenKeys", bindJwtSettings);
        services.AddSingleton(bindJwtSettings);
        services.AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options => {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters() {
                ValidateIssuerSigningKey = bindJwtSettings.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(bindJwtSettings.IssuerSigningKey)),
                ValidateIssuer = bindJwtSettings.ValidateIssuer,
                ValidIssuer = bindJwtSettings.ValidIssuer,
                ValidateAudience = bindJwtSettings.ValidateAudience,
                ValidAudience = bindJwtSettings.ValidAudience,
                RequireExpirationTime = bindJwtSettings.RequireExpirationTime,
                ValidateLifetime = bindJwtSettings.RequireExpirationTime,
                ClockSkew = TimeSpan.FromDays(1),
            };
        });
        return services;
    }
}