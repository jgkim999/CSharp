using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Serilog;
using StRunner.Exceptions;
using StRunner.Services;

internal class Program
{
    public static void Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSerilog();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddExceptionHandler<CustomExceptionHandler>();

            builder.Services.AddHealthChecks();

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("StRunner"))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter([
                        "Microsoft.AspNetCore.Hosting",
                        "Microsoft.AspNetCore.Server.Kestrel",
                        "Microsoft.AspNetCore.Diagnostics",
                        "System.Net.NameResolution",
                        "System.Runtime",
                        "Microsoft.Extensions.Diagnostics.HealthChecks",
                        "Microsoft.Extensions.Diagnostics.ResourceMonitoring",
                        "Microsoft.Extensions.Hosting",
                        "System.Http"
                    ])
                    .AddPrometheusExporter()
                );

            builder.Services.AddSingleton<K6Service>();

            // Add services to the container.
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/health");

            app.MapPrometheusScrapingEndpoint();

            app.Run();
        }
        catch (Exception e)
        {
            Log.Error(e, "Unhandled Exception");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
