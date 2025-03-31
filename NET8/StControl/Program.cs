using MudBlazor.Services;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

using Serilog;

using StControl.Components;
using StControl.Services;

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

            builder.Services.AddHealthChecks();

            builder.Services.AddMudServices();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

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

            builder.Services.AddHostedService<EcsBackgroundService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
            }

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

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
