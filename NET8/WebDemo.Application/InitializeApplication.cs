using Docker.DotNet;
using Docker.DotNet.Models;

using MassTransit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

using WebDemo.Application.WeatherService;
using WebDemo.Domain.Configs;

namespace WebDemo.Application;

public static class InitializeApplication
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string name, string version)
    {
        DisplayInfo();
        DisplayEnvValues();
        DisplayDockerInfoAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        var assembly = AppDomain.CurrentDomain.GetAssemblies();
        var entryAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(entryAssembly));
        
        //services.AddHostedService<Worker>();

        //services.AddAutoMapper(Assembly.GetExecutingAssembly());

        //services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        /*
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
        });
        */
        ActivityManager activityManager = new ActivityManager(name, version);
        services.AddSingleton<ActivityManager>(activityManager);

        return services;
    }

    private static void DisplayInfo()
    {
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        GlobalLogger.ForContext("DisplayInfo").Information("Local Time Zone ID:{Id}", localZone.Id);
        GlobalLogger.ForContext("DisplayInfo").Information("Display Name is:{DisplayName}.", localZone.DisplayName);
        GlobalLogger.ForContext("DisplayInfo").Information("Standard name is:{StandardName}.", localZone.StandardName);
        GlobalLogger.ForContext("DisplayInfo").Information("Daylight saving name is:{DaylightName}.", localZone.DaylightName);

        GlobalLogger.ForContext("DisplayInfo").Information("Environment UserName:{UserName}", Environment.UserName);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment MachineName:{MachineName}", Environment.MachineName);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment OSVersion:{OSVersion}", Environment.OSVersion);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment Version:{Version}", Environment.Version);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment CurrentDirectory:{EnvironmentCurrentDirectory}", Environment.CurrentDirectory);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment SystemDirectory:{EnvironmentSystemDirectory}", Environment.SystemDirectory);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment UserDomainName:{EnvironmentUserDomainName}", Environment.UserDomainName);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment UserInteractive:{EnvironmentUserInteractive}", Environment.UserInteractive);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment ProcessorCount:{EnvironmentProcessorCount}", Environment.ProcessorCount);
        GlobalLogger.ForContext("DisplayInfo").Information("Environment ASPNETCORE_ENVIRONMENT:{EnvironmentVariable}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    }

    private static void DisplayEnvValues()
    {
        IOrderedEnumerable<DictionaryEntry>? sortedEntries = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(entry => entry.Key);
        if (sortedEntries is null)
            return;

        int maxKeyLen = sortedEntries.Max(entry => ((string)entry.Key).Length);
        foreach (var entry in sortedEntries)
        {
            GlobalLogger.ForContext("DisplayInfo").Information("Environment {EnvKey}:{EnvValue}", entry.Key, entry.Value);
        }
    }

    private static async Task DisplayDockerInfoAsync()
    {
        try
        {
            // get container id
            var name = Dns.GetHostName();
            GlobalLogger.ForContext("DisplayInfo").Information("Container Name:{ContainerName}", name);

            var ip = (await Dns.GetHostEntryAsync(name)).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            GlobalLogger.ForContext("DisplayInfo").Information("Container Ip:{ip}", ip?.ToString());

            DockerClient client = new DockerClientConfiguration().CreateClient();
            SystemInfoResponse res = await client.System.GetSystemInfoAsync();

            GlobalLogger.ForContext("DisplayInfo").Information("Container OS Version:{OSVersion}", res.OSVersion);
            GlobalLogger.ForContext("DisplayInfo").Information("Container Architecture:{Architecture}", res.Architecture);
            GlobalLogger.ForContext("DisplayInfo").Information("Container MemoryLimit:{MemoryLimit}", res.MemoryLimit);
            GlobalLogger.ForContext("DisplayInfo").Information("Container MemTotal:{MemTotal}", res.MemTotal);
        }
        catch (Exception e)
        {
            GlobalLogger.ForContext("DisplayInfo").Error(e, "Error during docker client creation");
        }
    }

    public static IServiceCollection AddApplicationOpenTelemetry(
        this IServiceCollection services,
        string serviceName,
        string seqApiKey)
    {
        var jaegerConfig = services.GetConfiguration().GetSection("Jaeger").Get<JaegerConfig>();
        if (jaegerConfig is null)
            throw new ArgumentNullException("Jaeger config is null");

        services.AddSingleton(jaegerConfig);

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
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
                    "System.Http"])
                .AddPrometheusExporter()
            )
            .WithTracing(tracing =>
            {
                tracing.AddSource(serviceName);
                tracing.AddSource("MassTransit");
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();

                // Jaeger
                tracing.AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri(jaegerConfig.AgentHost);
                    //opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                });
                /*
                // Seq
                tracing.AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri("http://192.168.0.47:10001/ingest/otlp/v1/traces");
                    opt.Protocol = OtlpExportProtocol.HttpProtobuf;
                    opt.Headers = $"X-Seq-ApiKey={seqApiKey}";
                });
                
                // Tempo
                tracing.AddOtlpExporter(opt =>
                {
                    opt.Endpoint = new Uri("http://192.168.0.47:4317");
                    opt.Protocol = OtlpExportProtocol.Grpc;
                    opt.Headers = $"X-Scope-OrgID={serviceName}";
                });
                */
            });
        return services;
    }
}
