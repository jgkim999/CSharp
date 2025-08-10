using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// 부하 테스트 성능 벤치마크
/// 높은 동시성 상황에서 OpenTelemetry의 성능 영향을 측정합니다
/// </summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 10)]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class LoadTestBenchmark
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
    /// OpenTelemetry 없이 동시 요청 처리 성능을 측정합니다 (낮은 부하)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark(Baseline = true)]
    public async Task<LoadTestResult> LowConcurrencyWithoutOtel()
    {
        return await RunLoadTest(_clientWithoutOtel!, concurrency: 10, totalRequests: 100);
    }

    /// <summary>
    /// OpenTelemetry와 함께 동시 요청 처리 성능을 측정합니다 (낮은 부하)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> LowConcurrencyWithOtel()
    {
        return await RunLoadTest(_clientWithOtel!, concurrency: 10, totalRequests: 100);
    }

    /// <summary>
    /// OpenTelemetry 없이 동시 요청 처리 성능을 측정합니다 (중간 부하)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> MediumConcurrencyWithoutOtel()
    {
        return await RunLoadTest(_clientWithoutOtel!, concurrency: 50, totalRequests: 500);
    }

    /// <summary>
    /// OpenTelemetry와 함께 동시 요청 처리 성능을 측정합니다 (중간 부하)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> MediumConcurrencyWithOtel()
    {
        return await RunLoadTest(_clientWithOtel!, concurrency: 50, totalRequests: 500);
    }

    /// <summary>
    /// OpenTelemetry 없이 동시 요청 처리 성능을 측정합니다 (높은 부하)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> HighConcurrencyWithoutOtel()
    {
        return await RunLoadTest(_clientWithoutOtel!, concurrency: 100, totalRequests: 1000);
    }

    /// <summary>
    /// OpenTelemetry와 함께 동시 요청 처리 성능을 측정합니다 (높은 부하)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> HighConcurrencyWithOtel()
    {
        return await RunLoadTest(_clientWithOtel!, concurrency: 100, totalRequests: 1000);
    }

    /// <summary>
    /// 혼합 워크로드 테스트를 수행합니다 (OpenTelemetry 없음)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> MixedWorkloadWithoutOtel()
    {
        return await RunMixedWorkloadTest(_clientWithoutOtel!, concurrency: 25, totalRequests: 250);
    }

    /// <summary>
    /// 혼합 워크로드 테스트를 수행합니다 (OpenTelemetry 포함)
    /// </summary>
    /// <returns>부하 테스트 결과</returns>
    [Benchmark]
    public async Task<LoadTestResult> MixedWorkloadWithOtel()
    {
        return await RunMixedWorkloadTest(_clientWithOtel!, concurrency: 25, totalRequests: 250);
    }

    /// <summary>
    /// 부하 테스트를 실행합니다
    /// </summary>
    /// <param name="client">HTTP 클라이언트</param>
    /// <param name="concurrency">동시 실행 수</param>
    /// <param name="totalRequests">총 요청 수</param>
    /// <returns>부하 테스트 결과</returns>
    private async Task<LoadTestResult> RunLoadTest(HttpClient client, int concurrency, int totalRequests)
    {
        var stopwatch = Stopwatch.StartNew();
        var semaphore = new SemaphoreSlim(concurrency);
        var results = new ConcurrentBag<RequestResult>();
        var tasks = new List<Task>();

        for (int i = 0; i < totalRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    var response = await client.GetAsync("/health");
                    requestStopwatch.Stop();

                    results.Add(new RequestResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        ResponseTime = requestStopwatch.Elapsed,
                        StatusCode = (int)response.StatusCode
                    });

                    response.Dispose();
                }
                catch (Exception ex)
                {
                    results.Add(new RequestResult
                    {
                        IsSuccess = false,
                        ResponseTime = TimeSpan.Zero,
                        StatusCode = 0,
                        Error = ex.Message
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var successfulRequests = results.Where(r => r.IsSuccess).ToList();
        var failedRequests = results.Where(r => !r.IsSuccess).ToList();

        return new LoadTestResult
        {
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests.Count,
            FailedRequests = failedRequests.Count,
            TotalTime = stopwatch.Elapsed,
            RequestsPerSecond = totalRequests / stopwatch.Elapsed.TotalSeconds,
            AverageResponseTime = successfulRequests.Any() ? 
                TimeSpan.FromTicks((long)successfulRequests.Average(r => r.ResponseTime.Ticks)) : 
                TimeSpan.Zero,
            MinResponseTime = successfulRequests.Any() ? 
                successfulRequests.Min(r => r.ResponseTime) : 
                TimeSpan.Zero,
            MaxResponseTime = successfulRequests.Any() ? 
                successfulRequests.Max(r => r.ResponseTime) : 
                TimeSpan.Zero,
            P95ResponseTime = CalculatePercentile(successfulRequests.Select(r => r.ResponseTime).ToList(), 0.95),
            P99ResponseTime = CalculatePercentile(successfulRequests.Select(r => r.ResponseTime).ToList(), 0.99)
        };
    }

    /// <summary>
    /// 혼합 워크로드 테스트를 실행합니다 (GET, POST 요청 혼합)
    /// </summary>
    /// <param name="client">HTTP 클라이언트</param>
    /// <param name="concurrency">동시 실행 수</param>
    /// <param name="totalRequests">총 요청 수</param>
    /// <returns>부하 테스트 결과</returns>
    private async Task<LoadTestResult> RunMixedWorkloadTest(HttpClient client, int concurrency, int totalRequests)
    {
        var stopwatch = Stopwatch.StartNew();
        var semaphore = new SemaphoreSlim(concurrency);
        var results = new ConcurrentBag<RequestResult>();
        var tasks = new List<Task>();

        for (int i = 0; i < totalRequests; i++)
        {
            var requestIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    HttpResponseMessage response;

                    // 70% GET 요청, 30% POST 요청
                    if (requestIndex % 10 < 7)
                    {
                        response = await client.GetAsync("/health");
                    }
                    else
                    {
                        var userRequest = new
                        {
                            Email = $"test{Guid.NewGuid():N}@example.com",
                            Name = "Test User"
                        };
                        response = await client.PostAsJsonAsync("/api/v1/users", userRequest);
                    }

                    requestStopwatch.Stop();

                    results.Add(new RequestResult
                    {
                        IsSuccess = response.IsSuccessStatusCode,
                        ResponseTime = requestStopwatch.Elapsed,
                        StatusCode = (int)response.StatusCode
                    });

                    response.Dispose();
                }
                catch (Exception ex)
                {
                    results.Add(new RequestResult
                    {
                        IsSuccess = false,
                        ResponseTime = TimeSpan.Zero,
                        StatusCode = 0,
                        Error = ex.Message
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var successfulRequests = results.Where(r => r.IsSuccess).ToList();
        var failedRequests = results.Where(r => !r.IsSuccess).ToList();

        return new LoadTestResult
        {
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests.Count,
            FailedRequests = failedRequests.Count,
            TotalTime = stopwatch.Elapsed,
            RequestsPerSecond = totalRequests / stopwatch.Elapsed.TotalSeconds,
            AverageResponseTime = successfulRequests.Any() ? 
                TimeSpan.FromTicks((long)successfulRequests.Average(r => r.ResponseTime.Ticks)) : 
                TimeSpan.Zero,
            MinResponseTime = successfulRequests.Any() ? 
                successfulRequests.Min(r => r.ResponseTime) : 
                TimeSpan.Zero,
            MaxResponseTime = successfulRequests.Any() ? 
                successfulRequests.Max(r => r.ResponseTime) : 
                TimeSpan.Zero,
            P95ResponseTime = CalculatePercentile(successfulRequests.Select(r => r.ResponseTime).ToList(), 0.95),
            P99ResponseTime = CalculatePercentile(successfulRequests.Select(r => r.ResponseTime).ToList(), 0.99)
        };
    }

    /// <summary>
    /// 응답 시간의 백분위수를 계산합니다
    /// </summary>
    /// <param name="responseTimes">응답 시간 목록</param>
    /// <param name="percentile">백분위수 (0.0 ~ 1.0)</param>
    /// <returns>해당 백분위수의 응답 시간</returns>
    private TimeSpan CalculatePercentile(List<TimeSpan> responseTimes, double percentile)
    {
        if (!responseTimes.Any())
            return TimeSpan.Zero;

        var sortedTimes = responseTimes.OrderBy(t => t.Ticks).ToList();
        var index = (int)Math.Ceiling(percentile * sortedTimes.Count) - 1;
        index = Math.Max(0, Math.Min(index, sortedTimes.Count - 1));
        
        return sortedTimes[index];
    }
}

/// <summary>
/// 개별 요청 결과를 담는 구조체
/// </summary>
public struct RequestResult
{
    public int ClientId { get; set; }
    public int RequestId { get; set; }
    public bool IsSuccess { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public int StatusCode { get; set; }
    public string? Error { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// 부하 테스트 결과를 담는 구조체
/// </summary>
public struct LoadTestResult
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public int RateLimitedRequests { get; set; }
    public TimeSpan TotalTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
}