using Docker.DotNet;
using Docker.DotNet.Models;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Demo.Application;

public static class ApplicationInitialize
{
    public static WebApplicationBuilder AddApplicationServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        DisplayEnvValues();
        DisplayDockerInfoAsync().GetAwaiter().GetResult();

        builder.Logging.AddOpenTelemetry(x =>
        {
            x.IncludeScopes = true;
            x.IncludeFormattedMessage = true;
        });
        builder.Services.AddSerilog();

        builder.Services.AddProblemDetails();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance =
                    $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

                Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
            };
        });

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
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
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
            });

            builder.Services.AddHealthChecks();
            {
                builder.WebHost.ConfigureKestrel((context, serverOptions) =>
                {
                    var kestrelSection = context.Configuration.GetSection("Kestrel");
                    serverOptions.Configure(kestrelSection);
                });
            }
        return builder;
    }

    private static void DisplayEnvValues()
    {
        IOrderedEnumerable<DictionaryEntry>? sortedEntries = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>().OrderBy(entry => entry.Key);
        if (sortedEntries is null)
            return;

        int maxKeyLen = sortedEntries.Max(entry => ((string)entry.Key).Length);
        foreach (var entry in sortedEntries)
        {
            Log.Logger.Information("Environment {EnvKey}:{EnvValue}", entry.Key, entry.Value);
        }
    }

    private static async Task DisplayDockerInfoAsync()
    {
        try
        {
            // get container id
            string name = Dns.GetHostName();
            Log.Logger.Information("Container Name:{ContainerName}", name);

            var address = (await Dns.GetHostEntryAsync(name)).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);

            IPAddress? ip = (await Dns.GetHostEntryAsync(name)).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
            Log.Logger.Information("Container Ip:{ip}", ip?.ToString());
            
            DockerClient client = new DockerClientConfiguration().CreateClient();
            SystemInfoResponse res = await client.System.GetSystemInfoAsync();

            Log.Logger.Information("Container OS Version:{OSVersion}", res.OSVersion);
            Log.Logger.Information("Container Architecture:{Architecture}", res.Architecture);
            Log.Logger.Information("Container MemoryLimit:{MemoryLimit}", res.MemoryLimit);
            Log.Logger.Information("Container MemTotal:{MemTotal}", res.MemTotal);
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Error during docker client creation");
        }
    }

    private static void Aspire()
    {
        var apiServiceBaseAddress = Environment.GetEnvironmentVariable("services:apiservice:https:0");
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiServiceBaseAddress)
        };
        //var apiClient = new ApiClient(httpClient);
    }
}
