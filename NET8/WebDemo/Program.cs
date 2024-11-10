using Consul;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Quartz;

using Serilog;
using Serilog.Core;

using WebDemo.Application.Services;
using WebDemo.Domain.Configs;

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
            .WriteTo.Console()
            .CreateLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSerilog();

            TimeZoneInfo localZone = TimeZoneInfo.Local;
            Log.Information("Local Time Zone ID:{0}", localZone.Id);
            Log.Information("Display Name is:{0}.", localZone.DisplayName);
            Log.Information("Standard name is:{0}.", localZone.StandardName);
            Log.Information("Daylight saving name is:{0}.", localZone.DaylightName);
            
            Log.Information("UserName:{0}", Environment.UserName);
            Log.Information("MachineName:{0}", Environment.MachineName);
            Log.Information("OSVersion:{0}", Environment.OSVersion);
            Log.Information("Version:{0}", Environment.Version);
            Log.Information("CurrentDirectory:{0}", Environment.CurrentDirectory);
            Log.Information("SystemDirectory:{0}", Environment.SystemDirectory);
            Log.Information("UserDomainName:{0}", Environment.UserDomainName);
            Log.Information("UserInteractive:{0}", Environment.UserInteractive);
            Log.Information("ProcessorCount:{0}", Environment.ProcessorCount);
            Log.Information("ASPNETCORE_ENVIRONMENT:{0}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            
            var otel = builder.Services.AddOpenTelemetry();
            otel.WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting")
                .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                .AddMeter("Microsoft.AspNetCore.Diagnostics")
                .AddMeter("System.Net.NameResolution")
                .AddMeter("System.Runtime")
                .AddMeter("Microsoft.Extensions.Diagnostics.HealthChecks")
                .AddMeter("Microsoft.Extensions.Diagnostics.ResourceMonitoring")
                .AddMeter("Microsoft.Extensions.Hosting")
                .AddMeter("System.Http")
                .AddPrometheusExporter());
            
            otel.WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                /*
                tracing.AddSource(greeterActivitySource.Name);
                if (tracingOtlpEndpoint != null)
                {
                    tracing.AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint);
                    });
                }
                else
                {
                    tracing.AddConsoleExporter();
                }
                */
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
