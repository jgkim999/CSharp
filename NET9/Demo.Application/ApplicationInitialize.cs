using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

using System.Collections;
using System.Diagnostics;

namespace Demo.Application;

public static class ApplicationInitialize
{
    public static WebApplicationBuilder AddApplicationServices(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        DisplayEnvValues();

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
}
