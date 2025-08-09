using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// 애플리케이션 시작 시간 성능 벤치마크
/// OpenTelemetry 도입 전후의 시작 시간을 비교 측정합니다
/// </summary>
[SimpleJob(RunStrategy.ColdStart, iterationCount: 10)]
[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class ApplicationStartupBenchmark
{
    /// <summary>
    /// OpenTelemetry가 비활성화된 상태에서 애플리케이션 시작 시간을 측정합니다
    /// </summary>
    /// <returns>시작 완료까지의 시간</returns>
    [Benchmark(Baseline = true)]
    public async Task<TimeSpan> StartupWithoutOpenTelemetry()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // OpenTelemetry 서비스 제거
                    var openTelemetryDescriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("OpenTelemetry") == true)
                        .ToList();
                    
                    foreach (var descriptor in openTelemetryDescriptors)
                    {
                        services.Remove(descriptor);
                    }
                });
                
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

        try
        {
            // 애플리케이션 시작 및 첫 번째 요청까지의 시간 측정
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    /// <summary>
    /// OpenTelemetry가 활성화된 상태에서 애플리케이션 시작 시간을 측정합니다
    /// </summary>
    /// <returns>시작 완료까지의 시간</returns>
    [Benchmark]
    public async Task<TimeSpan> StartupWithOpenTelemetry()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

        try
        {
            // 애플리케이션 시작 및 첫 번째 요청까지의 시간 측정
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    /// <summary>
    /// 개발 환경 설정으로 애플리케이션 시작 시간을 측정합니다
    /// </summary>
    /// <returns>시작 완료까지의 시간</returns>
    [Benchmark]
    public async Task<TimeSpan> StartupWithDevelopmentConfig()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Information);
                });
            });

        try
        {
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }

    /// <summary>
    /// 프로덕션 환경 설정으로 애플리케이션 시작 시간을 측정합니다
    /// </summary>
    /// <returns>시작 완료까지의 시간</returns>
    [Benchmark]
    public async Task<TimeSpan> StartupWithProductionConfig()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureLogging(logging =>
                {
                    logging.SetMinimumLevel(LogLevel.Warning);
                });
            });

        try
        {
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health");
            
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}