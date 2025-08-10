using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Demo.Web.Endpoints.User;
using Demo.Web.PerformanceTests.Models;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// Rate Limiting의 동시 요청 처리 성능을 측정하는 부하 테스트 벤치마크
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(Config))]
public class RateLimitingLoadTestBenchmark
{
    private WebApplicationFactory<Program>? _factory;
    private string? _requestJson;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddDiagnoser(new EventPipeProfiler(EventPipeProfile.CpuSampling));
        }
    }

    /// <summary>
    /// 벤치마크 실행 전 초기화를 수행합니다.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();

        // 테스트용 요청 데이터 준비
        var request = new UserCreateRequest
        {
            Name = "LoadTestUser",
            Email = "loadtest@example.com",
            Password = "password123"
        };

        _requestJson = JsonSerializer.Serialize(request);
    }

    /// <summary>
    /// 벤치마크 실행 후 정리를 수행합니다.
    /// </summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _factory?.Dispose();
    }

    /// <summary>
    /// 다수의 동시 요청에 대한 Rate Limiting 동작을 테스트합니다.
    /// <summary>
    /// Executes a concurrent load test against the user creation API endpoint, simulating multiple clients each sending a specified number of requests to evaluate rate limiting behavior.
    /// </summary>
    /// <param name="clientCount">The number of simulated clients making requests concurrently.</param>
    /// <param name="requestsPerClient">The number of requests each client sends.</param>
    /// <returns>A <see cref="LoadTestResult"/> summarizing request outcomes, response times, and rate limiting statistics.</returns>
    [Benchmark]
    [Arguments(10, 5)]   // 10개 클라이언트, 각각 5개 요청
    [Arguments(20, 10)]  // 20개 클라이언트, 각각 10개 요청
    [Arguments(50, 5)]   // 50개 클라이언트, 각각 5개 요청
    public async Task<LoadTestResult> ConcurrentRequests_WithRateLimit(int clientCount, int requestsPerClient)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<RequestResult>();
        var semaphore = new SemaphoreSlim(clientCount);

        var tasks = Enumerable.Range(0, clientCount).Select(async clientId =>
        {
            await semaphore.WaitAsync();
            try
            {
                using var client = _factory!.CreateClient();
                var clientIp = $"10.{clientId / 255}.{clientId % 255}.1";
                client.DefaultRequestHeaders.Add("X-Forwarded-For", clientIp);

                for (int requestId = 0; requestId < requestsPerClient; requestId++)
                {
                    var requestStopwatch = Stopwatch.StartNew();
                    try
                    {
                        var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
                        var response = await client.PostAsync("/api/user/create", content);
                        
                        requestStopwatch.Stop();
                        
                        results.Add(new RequestResult
                        {
                            ClientId = clientId,
                            RequestId = requestId,
                            StatusCode = (int)response.StatusCode,
                            ResponseTime = requestStopwatch.Elapsed,
                            Success = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.TooManyRequests
                        });
                    }
                    catch (Exception ex)
                    {
                        requestStopwatch.Stop();
                        results.Add(new RequestResult
                        {
                            ClientId = clientId,
                            RequestId = requestId,
                            StatusCode = (int)HttpStatusCode.InternalServerError,
                            ResponseTime = requestStopwatch.Elapsed,
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var resultsList = results.ToList();
        return new LoadTestResult
        {
            TotalRequests = resultsList.Count,
            SuccessfulRequests = resultsList.Count(r => r.Success),
            RateLimitedRequests = resultsList.Count(r => r.StatusCode == (int)HttpStatusCode.TooManyRequests),
            FailedRequests = resultsList.Count(r => !r.Success),
            TotalTime = stopwatch.Elapsed,
            AverageResponseTime = TimeSpan.FromMilliseconds(resultsList.Average(r => r.ResponseTime.TotalMilliseconds)),
            MaxResponseTime = resultsList.Max(r => r.ResponseTime),
            MinResponseTime = resultsList.Min(r => r.ResponseTime),
            RequestsPerSecond = resultsList.Count / stopwatch.Elapsed.TotalSeconds
        };
    }

    /// <summary>
    /// 단일 IP에서 Rate Limit을 초과하는 동시 요청을 테스트합니다.
    /// <summary>
    /// Executes a benchmark that sends multiple concurrent POST requests from the same IP address to the user creation endpoint, measuring rate limiting behavior when exceeding the allowed request rate.
    /// </summary>
    /// <param name="concurrentRequests">The number of simultaneous requests to send from the fixed IP address.</param>
    /// <returns>A <see cref="LoadTestResult"/> summarizing request outcomes, response times, and rate limiting statistics.</returns>
    [Benchmark]
    [Arguments(20)]  // 20개 동시 요청 (Rate Limit: 10)
    [Arguments(50)]  // 50개 동시 요청 (Rate Limit: 10)
    public async Task<LoadTestResult> ConcurrentRequests_SameIP_ExceedingRateLimit(int concurrentRequests)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<RequestResult>();
        var fixedIp = "192.168.200.100";

        var tasks = Enumerable.Range(0, concurrentRequests).Select(async requestId =>
        {
            var requestStopwatch = Stopwatch.StartNew();
            try
            {
                using var client = _factory!.CreateClient();
                client.DefaultRequestHeaders.Add("X-Forwarded-For", fixedIp);

                var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/user/create", content);
                
                requestStopwatch.Stop();
                
                results.Add(new RequestResult
                {
                    ClientId = 0,
                    RequestId = requestId,
                    StatusCode = (int)response.StatusCode,
                    ResponseTime = requestStopwatch.Elapsed,
                    Success = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.TooManyRequests
                });
            }
            catch (Exception ex)
            {
                requestStopwatch.Stop();
                results.Add(new RequestResult
                {
                    ClientId = 0,
                    RequestId = requestId,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    ResponseTime = requestStopwatch.Elapsed,
                    Success = false,
                    Error = ex.Message
                });
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var resultsList = results.ToList();
        return new LoadTestResult
        {
            TotalRequests = resultsList.Count,
            SuccessfulRequests = resultsList.Count(r => r.StatusCode == (int)HttpStatusCode.OK),
            RateLimitedRequests = resultsList.Count(r => r.StatusCode == (int)HttpStatusCode.TooManyRequests),
            FailedRequests = resultsList.Count(r => !r.Success),
            TotalTime = stopwatch.Elapsed,
            AverageResponseTime = TimeSpan.FromMilliseconds(resultsList.Average(r => r.ResponseTime.TotalMilliseconds)),
            MaxResponseTime = resultsList.Max(r => r.ResponseTime),
            MinResponseTime = resultsList.Min(r => r.ResponseTime),
            RequestsPerSecond = resultsList.Count / stopwatch.Elapsed.TotalSeconds
        };
    }

    /// <summary>
    /// 메모리 사용량 집약적인 시나리오를 테스트합니다.
    /// <summary>
    /// Simulates a memory-intensive load test by sending concurrent POST requests from 100 unique IP addresses to the user creation endpoint.
    /// </summary>
    /// <returns>A <see cref="LoadTestResult"/> summarizing request outcomes, response times, and rate limiting statistics.</returns>
    [Benchmark]
    public async Task<LoadTestResult> MemoryIntensive_MultipleIPs()
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<RequestResult>();
        var ipCount = 100; // 100개의 서로 다른 IP
        var requestsPerIp = 5; // 각 IP당 5개 요청

        var tasks = Enumerable.Range(0, ipCount).Select(async ipIndex =>
        {
            using var client = _factory!.CreateClient();
            var clientIp = $"172.16.{ipIndex / 255}.{ipIndex % 255}";
            client.DefaultRequestHeaders.Add("X-Forwarded-For", clientIp);

            for (int requestId = 0; requestId < requestsPerIp; requestId++)
            {
                var requestStopwatch = Stopwatch.StartNew();
                try
                {
                    var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/api/user/create", content);
                    
                    requestStopwatch.Stop();
                    
                    results.Add(new RequestResult
                    {
                        ClientId = ipIndex,
                        RequestId = requestId,
                        StatusCode = (int)response.StatusCode,
                        ResponseTime = requestStopwatch.Elapsed,
                        Success = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.TooManyRequests
                    });
                }
                catch (Exception ex)
                {
                    requestStopwatch.Stop();
                    results.Add(new RequestResult
                    {
                        ClientId = ipIndex,
                        RequestId = requestId,
                        StatusCode = (int)HttpStatusCode.InternalServerError,
                        ResponseTime = requestStopwatch.Elapsed,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var resultsList = results.ToList();
        return new LoadTestResult
        {
            TotalRequests = resultsList.Count,
            SuccessfulRequests = resultsList.Count(r => r.Success),
            RateLimitedRequests = resultsList.Count(r => r.StatusCode == (int)HttpStatusCode.TooManyRequests),
            FailedRequests = resultsList.Count(r => !r.Success),
            TotalTime = stopwatch.Elapsed,
            AverageResponseTime = TimeSpan.FromMilliseconds(resultsList.Average(r => r.ResponseTime.TotalMilliseconds)),
            MaxResponseTime = resultsList.Max(r => r.ResponseTime),
            MinResponseTime = resultsList.Min(r => r.ResponseTime),
            RequestsPerSecond = resultsList.Count / stopwatch.Elapsed.TotalSeconds
        };
    }
}
