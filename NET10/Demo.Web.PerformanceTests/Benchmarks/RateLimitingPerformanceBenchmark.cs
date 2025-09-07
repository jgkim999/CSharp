using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Demo.Application.Configs;
using Demo.Application.Models;
using Demo.Web.Endpoints.User;

namespace Demo.Web.PerformanceTests.Benchmarks;

/// <summary>
/// Rate Limiting이 성능에 미치는 영향을 측정하는 벤치마크 클래스
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
[Config(typeof(Config))]
public class RateLimitingPerformanceBenchmark
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private HttpClient? _clientWithoutRateLimit;
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
        // Rate Limiting이 활성화된 팩토리
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();

        // Rate Limiting이 비활성화된 팩토리
        var factoryWithoutRateLimit = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Rate Limiting 비활성화 설정
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RateLimitConfig));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    var disabledConfig = new RateLimitConfig
                    {
                        UserCreateEndpoint = new UserCreateEndpointConfig
                        {
                            Enabled = false,
                            HitLimit = 10,
                            DurationSeconds = 60
                        }
                    };
                    services.AddSingleton(disabledConfig);
                });
                builder.UseEnvironment("Testing");
            });

        _clientWithoutRateLimit = factoryWithoutRateLimit.CreateClient();

        // 테스트용 요청 데이터 준비
        var request = new UserCreateRequest
        {
            Name = "BenchmarkUser",
            Email = "benchmark@example.com",
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
        _client?.Dispose();
        _clientWithoutRateLimit?.Dispose();
        _factory?.Dispose();
    }

    /// <summary>
    /// Rate Limiting이 활성화된 상태에서 단일 요청의 성능을 측정합니다.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<HttpStatusCode> SingleRequest_WithRateLimit()
    {
        var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
        
        // 각 요청마다 다른 IP를 사용하여 Rate Limit에 걸리지 않도록 함
        var randomIp = $"192.168.1.{Random.Shared.Next(1, 255)}";
        _client!.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("X-Forwarded-For", randomIp);
        
        var response = await _client.PostAsync("/api/user/create", content);
        return response.StatusCode;
    }

    /// <summary>
    /// Rate Limiting이 비활성화된 상태에서 단일 요청의 성능을 측정합니다.
    /// </summary>
    [Benchmark]
    public async Task<HttpStatusCode> SingleRequest_WithoutRateLimit()
    {
        var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
        var response = await _clientWithoutRateLimit!.PostAsync("/api/user/create", content);
        return response.StatusCode;
    }

    /// <summary>
    /// Rate Limiting이 활성화된 상태에서 연속 요청의 성능을 측정합니다.
    /// </summary>
    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    public async Task<List<HttpStatusCode>> ConsecutiveRequests_WithRateLimit(int requestCount)
    {
        var results = new List<HttpStatusCode>();
        var baseIp = Random.Shared.Next(1, 200);

        for (int i = 0; i < requestCount; i++)
        {
            var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
            
            // 각 요청마다 다른 IP를 사용
            var ip = $"10.0.{baseIp}.{i + 1}";
            _client!.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("X-Forwarded-For", ip);
            
            var response = await _client.PostAsync("/api/user/create", content);
            results.Add(response.StatusCode);
        }

        return results;
    }

    /// <summary>
    /// Rate Limiting이 비활성화된 상태에서 연속 요청의 성능을 측정합니다.
    /// </summary>
    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    public async Task<List<HttpStatusCode>> ConsecutiveRequests_WithoutRateLimit(int requestCount)
    {
        var results = new List<HttpStatusCode>();

        for (int i = 0; i < requestCount; i++)
        {
            var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
            var response = await _clientWithoutRateLimit!.PostAsync("/api/user/create", content);
            results.Add(response.StatusCode);
        }

        return results;
    }

    /// <summary>
    /// Rate Limit에 도달한 상태에서의 성능을 측정합니다.
    /// </summary>
    [Benchmark]
    public async Task<List<HttpStatusCode>> RateLimitExceeded_Performance()
    {
        var results = new List<HttpStatusCode>();
        var fixedIp = "192.168.100.100";

        // 동일한 IP로 Rate Limit을 초과하여 요청
        for (int i = 0; i < 15; i++)
        {
            var content = new StringContent(_requestJson!, Encoding.UTF8, "application/json");
            _client!.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("X-Forwarded-For", fixedIp);
            
            var response = await _client.PostAsync("/api/user/create", content);
            results.Add(response.StatusCode);
        }

        return results;
    }
}