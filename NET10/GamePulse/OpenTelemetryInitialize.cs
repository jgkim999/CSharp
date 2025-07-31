using GamePulse.Configs;
using GamePulse.Services;

using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GamePulse;

/// <summary>
/// 
/// </summary>
public static class OpenTelemetryInitialize
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="service"></param>
    /// <param name="config"></param>
    /// <summary>
    /// Configures and adds OpenTelemetry tracing and metrics services to the dependency injection container using the specified configuration.
    /// </summary>
    /// <param name="config">The OpenTelemetry configuration settings, including service name, version, endpoint, and trace sampling argument.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> with OpenTelemetry services registered.</returns>
    public static IServiceCollection AddOpenTelemetryServices(this IServiceCollection service, OtelConfig config)
    {
        var openTelemetryBuilder = service.AddOpenTelemetry();
        if (double.TryParse(config.TracesSamplerArg, out var probability) == false)
            probability = 1.0f;
        
        StackExchangeRedisInstrumentation? redisInstrumentation = null;

        GamePulseActivitySource.Initialize(config.ServiceName, config.ServiceVersion);
            
        openTelemetryBuilder.WithTracing(tracing =>
        {
            tracing.AddSource(config.ServiceName);
            tracing.SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: config.ServiceName, serviceVersion: config.ServiceVersion));
            tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddRedisInstrumentation()
                .ConfigureRedisInstrumentation(instrumentation => redisInstrumentation = instrumentation);
            tracing.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(config.Endpoint);
                o.Protocol = OtlpExportProtocol.Grpc;
            });
            //tracing.AddConsoleExporter();
        });

        if (redisInstrumentation is not null)
            openTelemetryBuilder.Services.AddSingleton(redisInstrumentation);
        
        openTelemetryBuilder.Services.AddSingleton(TracerProvider.Default.GetTracer(config.ServiceName));
        
        openTelemetryBuilder.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddRuntimeInstrumentation();
            metrics.AddHttpClientInstrumentation();
            // Metrics provides by ASP.NET Core in .NET 8
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            // Metrics provided by System.Net libraries
            metrics.AddMeter("System.Net.Http");
            metrics.AddMeter("System.Net.NameResolution");
            metrics.AddOtlpExporter(o =>
            {
                o.Endpoint = new Uri(config.Endpoint);
                o.Protocol = OtlpExportProtocol.Grpc;
            });
            //metrics.AddConsoleExporter();
        });

        //builder.WithLogging();
        /*
        builder.Logging.AddOpenTelemetry(logging => logging
            .AddConsoleExporter());
        */
        return service;
    }
}
