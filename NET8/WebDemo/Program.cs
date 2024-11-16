using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Quartz;

using Serilog;

using System.Diagnostics;

using WebDemo.Application;
using WebDemo.Infra;

internal class Program
{
    public static void Main(string[] args)
    {
        ActivitySource tracingSource = new("Example.Source");

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)           .CreateLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSerilog();

            
            
            var otel = builder.Services.AddOpenTelemetry()
                .ConfigureResource(r => r.AddService("My Service"))
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    /*
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("Microsoft.AspNetCore.Diagnostics")
                    .AddMeter("System.Net.NameResolution")
                    .AddMeter("System.Runtime")
                    .AddMeter("Microsoft.Extensions.Diagnostics.HealthChecks")
                    .AddMeter("Microsoft.Extensions.Diagnostics.ResourceMonitoring")
                    .AddMeter("Microsoft.Extensions.Hosting")
                    .AddMeter("System.Http")
                    */
                    .AddPrometheusExporter()
                    )
                .WithTracing(tracing =>
                {
                    tracing.AddSource("WebDemo");
                    tracing.AddSource("MassTransit");
                    tracing.AddAspNetCoreInstrumentation();
                    tracing.AddHttpClientInstrumentation();
                    tracing.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri("http://192.168.0.47:10001/ingest/otlp/v1/traces");
                        opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                        opt.Headers = "X-Seq-ApiKey=7IcnLMHBbZxPx03s2Plb";
                    });
                });
            
            builder.Services.AddHealthChecks();
            {
                builder.WebHost.ConfigureKestrel((context, serverOptions) =>
                {
                    var kestrelSection = context.Configuration.GetSection("Kestrel");
                    serverOptions.Configure(kestrelSection);
                });
                Log.Information("ASPNETCORE_ENVIRONMENT", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                Log.Information("ASPNETCORE_URLS", Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
            }

            // Add services to the container.
            builder.Services.AddQuartz();
            builder.Services.AddQuartzHostedService(opt => { opt.WaitForJobsToComplete = true; });

            // Consul
            {
                /*
                var consulConfig = builder.Configuration.GetSection("Consul").Get<ConsulConfig>();
                builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(
                    e =>
                {
                    e.Address = new Uri(consulConfig.Host);
                }));
                builder.Services.AddSingleton(consulConfig);
                builder.Services.AddSingleton<IHostedService, ConsulHostedService>();
                */
            }
            
            builder.Services.AddApplicationServices("WebDemo", "1.0.0");
            builder.Services.AddInfraServices();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseSerilogRequestLogging();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}

            app.UseAuthorization();

            app.MapControllers();

            app.MapHealthChecks("/healthz");

            app.MapPrometheusScrapingEndpoint();
            
            app.Run();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
