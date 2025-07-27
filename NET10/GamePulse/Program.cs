using FastEndpoints;
using FastEndpoints.Security;
using GamePulse;
using GamePulse.Configs;
using GamePulse.Repositories.Jwt;
using GamePulse.Services;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
try
{
    var otelConfig = builder.Configuration.GetSection("OpenTelemetry").Get<OtelConfig>();
    if (otelConfig is null)
        throw new NullReferenceException();
    
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());
    
    Log.Information("Starting application");
    
    var section = builder.Configuration.GetSection("Jwt");
    var jwtConfig = section.Get<JwtConfig>();
    if (jwtConfig == null)
    {
        throw new NullReferenceException();
    }

    builder.Services.AddAuthenticationJwtBearer(s => s.SigningKey = jwtConfig.PublicKey);
    builder.Services.AddAuthorization();

    builder.Services.Configure<JwtCreationOptions>(o => o.SigningKey = jwtConfig.PrivateKey);

    builder.Services.AddFastEndpoints();

    builder.Services.AddOpenApiServices();
    
    builder.Services.AddSingleton<IAuthService, AuthService>();
    builder.Services.AddTransient<IJwtRepository, RedisJwtRepository>();

    builder.Services.AddOpenTelemetryServices(otelConfig);

    var app = builder.Build();
    app.UseAuthentication();
    app.UseAuthentication();
    app.UseFastEndpoints(c => { c.Versioning.Prefix = "v"; });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
        string[] versions = ["v1", "v2"];
        app.MapScalarApiReference(options => options.AddDocuments(versions));
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
