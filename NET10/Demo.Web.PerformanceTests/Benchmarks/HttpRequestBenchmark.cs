using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// HTTP 요청 처리 성능 벤치마크
/// OpenTelemetry 도입으로 인한 HTTP 요청 처리 오버헤드를 측정합니다
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 100)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class HttpRequestBenchmark
{
    private WebApplicationFactory<Program>? _factoryWithoutOtel;
    private WebApplicationFactory<Program>? _factoryWithOtel;
    private HttpClient? _clientWithoutOtel;
    private HttpClient? _clientWithOtel;

    /// <summary>
    /// 벤치마크 실행 전 초기화를 수행합니다
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // OpenTelemetry 없는 팩토리 설정
        _factoryWithoutOtel = new WebApplicationFactory<Program>()
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
                    logging.SetMinimumLevel(LogLevel.Error);
                });
            });

        // OpenTelemetry 있는 팩토리 설정
        _factoryWithOtel = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Error);
                });
            });

        _clientWithoutOtel = _factoryWithoutOtel.CreateClient();
        _clientWithOtel = _factoryWithOtel.CreateClient();
    }

    /// <summary>
    /// 벤치마크 실행 후 정리를 수행합니다
    /// </summary>
    [GlobalCleanup]
    public async Task Cleanup()
    {
        _clientWithoutOtel?.Dispose();
        _clientWithOtel?.Dispose();
        
        if (_factoryWithoutOtel != null)
            await _factoryWithoutOtel.DisposeAsync();
        
        if (_factoryWithOtel != null)
            await _factoryWithOtel.DisposeAsync();
    }

    /// <summary>
    /// OpenTelemetry 없이 간단한 GET 요청 성능을 측정합니다
    /// </summary>
    /// <returns>HTTP 응답</returns>
    [Benchmark(Baseline = true)]
    public async Task<HttpResponseMessage> SimpleGetRequestWithoutOtel()
    {
        return await _clientWithoutOtel!.GetAsync("/health");
    }

    /// <summary>
    /// OpenTelemetry와 함께 간단한 GET 요청 성능을 측정합니다
    /// </summary>
    /// <returns>HTTP 응답</returns>
    [Benchmark]
    public async Task<HttpResponseMessage> SimpleGetRequestWithOtel()
    {
        return await _clientWithOtel!.GetAsync("/health");
    }

    /// <summary>
    /// OpenTelemetry 없이 사용자 생성 POST 요청 성능을 측정합니다
    /// </summary>
    /// <returns>HTTP 응답</returns>
    [Benchmark]
    public async Task<HttpResponseMessage> UserCreateRequestWithoutOtel()
    {
        var userRequest = new
        {
            Email = $"test{Guid.NewGuid():N}@example.com",
            Name = "Test User"
        };

        return await _clientWithoutOtel!.PostAsJsonAsync("/api/v1/users", userRequest);
    }

    /// <summary>
    /// OpenTelemetry와 함께 사용자 생성 POST 요청 성능을 측정합니다
    /// </summary>
    /// <returns>HTTP 응답</returns>
    [Benchmark]
    public async Task<HttpResponseMessage> UserCreateRequestWithOtel()
    {
        var userRequest = new
        {
            Email = $"test{Guid.NewGuid():N}@example.com",
            Name = "Test User"
        };

        return await _clientWithOtel!.PostAsJsonAsync("/api/v1/users", userRequest);
    }

    /// <summary>
    /// OpenTelemetry 없이 여러 연속 요청 성능을 측정합니다
    /// </summary>
    /// <returns>모든 응답의 배열</returns>
    [Benchmark]
    public async Task<HttpResponseMessage[]> MultipleRequestsWithoutOtel()
    {
        var tasks = new Task<HttpResponseMessage>[10];
        
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _clientWithoutOtel!.GetAsync("/health");
        }

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// OpenTelemetry와 함께 여러 연속 요청 성능을 측정합니다
    /// </summary>
    /// <returns>모든 응답의 배열</returns>
    [Benchmark]
    public async Task<HttpResponseMessage[]> MultipleRequestsWithOtel()
    {
        var tasks = new Task<HttpResponseMessage>[10];
        
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _clientWithOtel!.GetAsync("/health");
        }

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 큰 페이로드를 포함한 요청의 성능을 측정합니다 (OpenTelemetry 없음)
    /// </summary>
    /// <returns>HTTP 응답</returns>
    [Benchmark]
    public async Task<HttpResponseMessage> LargePayloadRequestWithoutOtel()
    {
        var largeData = new
        {
            Data = string.Join("", Enumerable.Repeat("A", 10000)),
            Items = Enumerable.Range(1, 100).Select(i => new { Id = i, Value = $"Item{i}" }).ToArray()
        };

        return await _clientWithoutOtel!.PostAsJsonAsync("/api/test/large-payload", largeData);
    }

    /// <summary>
    /// 큰 페이로드를 포함한 요청의 성능을 측정합니다 (OpenTelemetry 포함)
    /// </summary>
    /// <returns>HTTP 응답</returns>
    [Benchmark]
    public async Task<HttpResponseMessage> LargePayloadRequestWithOtel()
    {
        var largeData = new
        {
            Data = string.Join("", Enumerable.Repeat("A", 10000)),
            Items = Enumerable.Range(1, 100).Select(i => new { Id = i, Value = $"Item{i}" }).ToArray()
        };

        return await _clientWithOtel!.PostAsJsonAsync("/api/test/large-payload", largeData);
    }
}