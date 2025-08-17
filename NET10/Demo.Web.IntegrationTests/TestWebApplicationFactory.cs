using Microsoft.AspNetCore.Mvc.Testing;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using System.Diagnostics;
using Demo.Application.Configs;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// OpenTelemetry 통합 테스트를 위한 사용자 정의 WebApplicationFactory
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public List<Activity> ExportedActivities { get; } = new();
    public List<Metric> ExportedMetrics { get; } = new();
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 기존 OpenTelemetry 관련 서비스들을 모두 제거
            var descriptorsToRemove = services.Where(d => 
                d.ServiceType.FullName?.Contains("OpenTelemetry") == true ||
                d.ServiceType == typeof(TracerProvider) ||
                d.ServiceType == typeof(MeterProvider)).ToList();
            
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 테스트용 Rate Limiting 설정 추가
            var rateLimitDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RateLimitConfig));
            if (rateLimitDescriptor == null)
            {
                var testRateLimitConfig = new RateLimitConfig
                {
                    UserCreateEndpoint = new UserCreateEndpointConfig
                    {
                        Enabled = true,
                        HitLimit = 10,
                        DurationSeconds = 60,
                        HeaderName = null,
                        ErrorMessage = "Too many requests. Please try again later.",
                        RetryAfterSeconds = 60
                    },
                    Global = new GlobalRateLimitConfig
                    {
                        EnableLogging = true,
                        LogRateLimitApplied = true,
                        LogRateLimitExceeded = true,
                        IncludeClientIpInLogs = true,
                        IncludeRequestCountInLogs = true
                    }
                };
                services.AddSingleton(testRateLimitConfig);
            }

            // 테스트용 OpenTelemetry 설정
            services.AddOpenTelemetry()
                .WithTracing(tracingBuilder =>
                {
                    tracingBuilder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService("Demo.Web.Test", "1.0.0", "test"))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddSource("Demo.Web")
                        .AddSource("Demo.Application") // TelemetryService에서 사용하는 소스
                        .AddInMemoryExporter(ExportedActivities);
                })
                .WithMetrics(metricsBuilder =>
                {
                    metricsBuilder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault()
                            .AddService("Demo.Web.Test", "1.0.0", "test"))
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter("Demo.Web")
                        .AddMeter("Demo.Application") // TelemetryService에서 사용하는 미터
                        .AddInMemoryExporter(ExportedMetrics);
                });
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// 내보낸 활동(Activity) 목록을 초기화합니다
    /// </summary>
    public void ClearExportedData()
    {
        ExportedActivities.Clear();
        ExportedMetrics.Clear();
    }
}
