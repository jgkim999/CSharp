using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Demo.Web.Endpoints.User;
using Demo.Web.PerformanceTests.Models;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// Rate Limiting의 메모리 사용량을 측정하는 벤치마크 클래스
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(Config))]
public class RateLimitingMemoryBenchmark
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
            Name = "MemoryTestUser",
            Email = "memorytest@example.com",
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
    /// 다수의 고유한 IP 주소로 요청하여 메모리 사용량을 측정합니다.
    /// </summary>
    [Benchmark]
    [Arguments(100)]   // 100개의 서로 다른 IP
    [Arguments(500)]   // 500개의 서로 다른 IP
    [Arguments(1000)]  // 1000개의 서로 다른 IP
    public async Task<MemoryTestResult> UniqueIPs_MemoryUsage(int uniqueIpCount)
    {
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        using var client = _factory!.CreateClient();
        var results = new List<HttpStatusCode>();

        // 각기 다른 IP로 요청하여 Rate Limiting 저장소에 항목 생성
        for (int i = 0; i < uniqueIpCount; i++)
        {
            var ip = $"10.{i / 65536}.{i / 256 % 256}.{i % 256}";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);

            var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/user/create", content);
            results.Add(response.StatusCode);

            // 매 100개마다 메모리 정리 시도
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        return new MemoryTestResult
        {
            UniqueIpCount = uniqueIpCount,
            TotalRequests = results.Count,
            SuccessfulRequests = results.Count(r => r == HttpStatusCode.OK),
            MemoryUsedBytes = memoryUsed,
            MemoryUsedMB = memoryUsed / (1024.0 * 1024.0),
            ExecutionTime = stopwatch.Elapsed,
            MemoryPerIP = memoryUsed / (double)uniqueIpCount
        };
    }

    /// <summary>
    /// 동일한 IP로 반복 요청하여 메모리 사용량을 측정합니다.
    /// </summary>
    [Benchmark]
    [Arguments(100)]   // 100개 요청
    [Arguments(500)]   // 500개 요청
    [Arguments(1000)]  // 1000개 요청
    public async Task<MemoryTestResult> SameIP_MemoryUsage(int requestCount)
    {
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        using var client = _factory!.CreateClient();
        var fixedIp = "192.168.100.200";
        client.DefaultRequestHeaders.Add("X-Forwarded-For", fixedIp);

        var results = new List<HttpStatusCode>();

        // 동일한 IP로 반복 요청 (Rate Limit에 걸릴 것임)
        for (int i = 0; i < requestCount; i++)
        {
            var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/user/create", content);
            results.Add(response.StatusCode);

            // 매 100개마다 메모리 정리 시도
            if (i % 100 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = finalMemory - initialMemory;

        return new MemoryTestResult
        {
            UniqueIpCount = 1,
            TotalRequests = results.Count,
            SuccessfulRequests = results.Count(r => r == HttpStatusCode.OK),
            RateLimitedRequests = results.Count(r => r == HttpStatusCode.TooManyRequests),
            MemoryUsedBytes = memoryUsed,
            MemoryUsedMB = memoryUsed / (1024.0 * 1024.0),
            ExecutionTime = stopwatch.Elapsed,
            MemoryPerIP = memoryUsed
        };
    }

    /// <summary>
    /// Rate Limiting 저장소의 메모리 누수를 확인합니다.
    /// </summary>
    [Benchmark]
    public async Task<MemoryTestResult> MemoryLeak_Detection()
    {
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        using var client = _factory!.CreateClient();
        var totalRequests = 0;
        var successfulRequests = 0;

        // 여러 라운드에 걸쳐 다양한 IP로 요청
        for (int round = 0; round < 10; round++)
        {
            // 각 라운드마다 100개의 서로 다른 IP 사용
            for (int i = 0; i < 100; i++)
            {
                var ip = $"172.{round}.{i / 256}.{i % 256}";
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);

                var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/user/create", content);
                
                totalRequests++;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    successfulRequests++;
                }
            }

            // 각 라운드 후 메모리 정리
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // 잠시 대기하여 Rate Limiting 윈도우가 만료되도록 함
            await Task.Delay(100);
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        return new MemoryTestResult
        {
            UniqueIpCount = 1000, // 10 라운드 × 100 IP
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            MemoryUsedBytes = memoryUsed,
            MemoryUsedMB = memoryUsed / (1024.0 * 1024.0),
            ExecutionTime = stopwatch.Elapsed,
            MemoryPerIP = memoryUsed / 1000.0
        };
    }

    /// <summary>
    /// 장시간 실행 시나리오에서의 메모리 사용량을 측정합니다.
    /// </summary>
    [Benchmark]
    public async Task<MemoryTestResult> LongRunning_MemoryUsage()
    {
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        using var client = _factory!.CreateClient();
        var totalRequests = 0;
        var successfulRequests = 0;
        var memorySnapshots = new List<long>();

        // 5분간 지속적으로 요청 (실제로는 더 짧은 시간으로 조정)
        var endTime = DateTime.UtcNow.AddSeconds(30); // 30초로 단축
        var ipCounter = 0;

        while (DateTime.UtcNow < endTime)
        {
            // 매번 새로운 IP 사용
            var ip = $"203.{ipCounter / 65536}.{ipCounter / 256 % 256}.{ipCounter % 256}";
            ipCounter++;

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);

            var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/user/create", content);
            
            totalRequests++;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                successfulRequests++;
            }

            // 매 50개 요청마다 메모리 스냅샷 저장
            if (totalRequests % 50 == 0)
            {
                memorySnapshots.Add(GC.GetTotalMemory(false));
            }

            // 짧은 대기
            await Task.Delay(10);
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = finalMemory - initialMemory;

        return new MemoryTestResult
        {
            UniqueIpCount = ipCounter,
            TotalRequests = totalRequests,
            SuccessfulRequests = successfulRequests,
            MemoryUsedBytes = memoryUsed,
            MemoryUsedMB = memoryUsed / (1024.0 * 1024.0),
            ExecutionTime = stopwatch.Elapsed,
            MemoryPerIP = ipCounter > 0 ? memoryUsed / (double)ipCounter : 0,
            MemorySnapshots = memorySnapshots
        };
    }
}

