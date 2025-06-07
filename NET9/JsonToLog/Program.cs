
using FastEndpoints;

using JsonToLog.Configs;
using JsonToLog.Features.LogSend;
using JsonToLog.Services;

using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile(
        $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
        optional: true)
    .AddEnvironmentVariables()
    .Build();

OpenTelemetryConfig? openTelemetryConfig = configuration.GetSection("OpenTelemetry").Get<OpenTelemetryConfig>();
if (openTelemetryConfig is null)
{
    throw new NullReferenceException("OpenTelemetry configuration is missing");
}

ApplicationConfig? applicationConfig = configuration.GetSection("ApplicationSettings").Get<ApplicationConfig>();
if (applicationConfig is null)
{
    throw new NullReferenceException("Application configuration is missing");
}

Dictionary<string, object> otelAttributes = new()
{
    { "service.name", applicationConfig.ServiceName },
    { "service.version", applicationConfig.Version },
    { "service.instance.id", Environment.MachineName },
    { "environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }
};

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", applicationConfig.ServiceName)
    .Enrich.WithProperty("ServiceVersion", applicationConfig.Version)
    .Enrich.WithProperty("ServiceInstanceId", Environment.MachineName)
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = openTelemetryConfig.Endpoint;
        options.Protocol = openTelemetryConfig.GetProtocol;
    })
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Environment.ApplicationName = applicationConfig.ServiceName;
    
    ActivityService.Initialize(applicationConfig.ServiceName, applicationConfig.Version);
    
    builder.Services.AddSerilog();
    
    var otel = builder.Services.AddOpenTelemetry();
    otel.ConfigureResource(resource => resource
        .AddService(applicationConfig.ServiceName));
    
    otel.WithMetrics(metrics =>
    {
        metrics.SetResourceBuilder(ResourceBuilder
            .CreateDefault()
            .AddService(applicationConfig.ServiceName));
        // Add custom ActivitySource for metrics
        metrics.AddMeter(LogSendMetrics.METER_NAME);
        
        // Metrics provider from OpenTelemetry
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddProcessInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddHttpClientInstrumentation();
        // Metrics provides by ASP.NET Core in .NET 8
        metrics.AddMeter("Microsoft.AspNetCore.Hosting");
        metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        // Metrics provided by System.Net libraries
        metrics.AddMeter("System.Net.Http");
        metrics.AddMeter("System.Net.NameResolution");
        //metrics.AddConsoleExporter();
        metrics.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(openTelemetryConfig.Endpoint);
            o.Protocol = OtlpExportProtocol.Grpc;
        });
        metrics.AddPrometheusExporter();
        //metrics.AddConsoleExporter();
    });
    
    // Add Tracing for ASP.NET Core and our custom ActivitySource and export to Jaeger
    otel.WithTracing(tracing =>
    {
        var probability = openTelemetryConfig.TraceSampleRate;
        tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
        tracing.AddSource(ActivityService.Name);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(openTelemetryConfig.Endpoint);
            o.Protocol = OtlpExportProtocol.Grpc;
        });
        //tracing.AddConsoleExporter();
    });

    builder.Services
        .AddFastEndpoints();

    builder.Services.AddSingleton<LogSendProcessor>();
    builder.Services.AddHostedService<LogSendProcessor>(p => p.GetRequiredService<LogSendProcessor>());
    builder.Services.AddSingleton<LogSendMetrics>();
    
    var app = builder.Build();

    app
        .UseDefaultExceptionHandler()
        .UseFastEndpoints(c =>
    {
        c.Versioning.Prefix = "v";
    });
    
    app.MapPrometheusScrapingEndpoint();
    
    app.Run();
}
catch (Exception e)
{
    Log.Fatal(e, "Ter");
}
finally
{
    Log.CloseAndFlush();
}
