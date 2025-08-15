using Demo.Web.Configs;
using GamePulse.Services;
using Demo.Application.Services;
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
    /// OpenTelemetry 추적 및 메트릭 서비스를 지정된 구성을 사용하여 의존성 주입 컨테이너에 구성하고 추가합니다.
    /// </summary>
    /// <param name="service">서비스 컬렉션</param>
    /// <param name="config">서비스 이름, 버전, 엔드포인트 및 추적 샘플링 인수를 포함한 OpenTelemetry 구성 설정</param>
    /// <returns>OpenTelemetry 서비스가 등록된 업데이트된 <see cref="IServiceCollection"/></returns>
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
            metrics.SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: config.ServiceName, serviceVersion: config.ServiceVersion));

            // TelemetryService의 MeterName을 OpenTelemetry에 등록
            // 서비스 이름을 사용하여 메트릭 수집 설정
            metrics.AddMeter(config.ServiceName);

            metrics.AddAspNetCoreInstrumentation();
            metrics.AddRuntimeInstrumentation();
            metrics.AddHttpClientInstrumentation();
            // Metrics provides by ASP.NET Core in .NET 8
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            // Metrics provided by System.Net libraries
            metrics.AddMeter("System.Net.Http");
            metrics.AddMeter("System.Net.NameResolution");
            metrics.AddOtlpExporter((options, readerOptions) =>
            {
                options.Endpoint = new Uri(config.Endpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000; // Set to 5 seconds
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
