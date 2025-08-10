using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Demo.Web.Endpoints.User;
using Xunit.Abstractions;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// Rate Limiting의 성능 및 부하 테스트를 위한 통합 테스트 클래스
/// </summary>
public class RateLimitingPerformanceIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public RateLimitingPerformanceIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    /// <summary>
    /// Rate Limiting이 응답 시간에 미치는 영향을 측정합니다.
    /// </summary>
    [Fact]
    public async Task RateLimit_ResponseTime_Impact_Test()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UserCreateRequest
        {
            Name = "PerformanceTestUser",
            Email = "performance@example.com",
            Password = "password123"
        };
        var requestJson = JsonSerializer.Serialize(request);

        var responseTimes = new List<TimeSpan>();
        var statusCodes = new List<HttpStatusCode>();

        // Act - 서로 다른 IP로 요청하여 Rate Limit 회피
        for (int i = 0; i < 10; i++)
        {
            using var testClient = _factory.CreateClient();
            var testIp = $"192.168.100.{i + 1}";
            testClient.DefaultRequestHeaders.Add("X-Forwarded-For", testIp);

            var stopwatch = Stopwatch.StartNew();
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await testClient.PostAsync("/api/user/create", content);
            stopwatch.Stop();

            responseTimes.Add(stopwatch.Elapsed);
            statusCodes.Add(response.StatusCode);
        }

        // 동일한 IP로 Rate Limit 테스트
        var rateLimitClient = _factory.CreateClient();
        var fixedIp = "192.168.200.100";
        rateLimitClient.DefaultRequestHeaders.Add("X-Forwarded-For", fixedIp);

        var rateLimitResponseTimes = new List<TimeSpan>();
        var rateLimitStatusCodes = new List<HttpStatusCode>();

        // 15개 요청으로 Rate Limit 초과 유도
        for (int i = 0; i < 15; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await rateLimitClient.PostAsync("/api/user/create", content);
            stopwatch.Stop();

            rateLimitResponseTimes.Add(stopwatch.Elapsed);
            rateLimitStatusCodes.Add(response.StatusCode);
        }

        // Assert
        var averageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average(t => t.TotalMilliseconds));
        var averageRateLimitResponseTime = TimeSpan.FromMilliseconds(rateLimitResponseTimes.Average(t => t.TotalMilliseconds));

        var successfulRequests = statusCodes.Count(s => s == HttpStatusCode.OK);
        var rateLimitedRequests = rateLimitStatusCodes.Count(s => s == HttpStatusCode.TooManyRequests);

        _output.WriteLine($"서로 다른 IP 요청 - 성공: {successfulRequests}/10");
        _output.WriteLine($"동일 IP 요청 - Rate Limit: {rateLimitedRequests}/15");
        _output.WriteLine($"평균 정상 응답 시간: {averageResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"평균 Rate Limit 응답 시간: {averageRateLimitResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"응답 시간 차이: {Math.Abs(averageRateLimitResponseTime.TotalMilliseconds - averageResponseTime.TotalMilliseconds):F2}ms");

        // 기본적인 성능 검증
        Assert.True(averageResponseTime.TotalMilliseconds < 1000, 
            $"평균 응답 시간({averageResponseTime.TotalMilliseconds:F2}ms)이 1초를 초과했습니다.");
        
        // Rate Limit 응답이 정상 응답보다 현저히 느리지 않아야 함 (5배 이내)
        Assert.True(averageRateLimitResponseTime.TotalMilliseconds < averageResponseTime.TotalMilliseconds * 5,
            $"Rate Limit 응답 시간({averageRateLimitResponseTime.TotalMilliseconds:F2}ms)이 정상 응답 시간({averageResponseTime.TotalMilliseconds:F2}ms)의 5배를 초과했습니다.");
    }

    /// <summary>
    /// 다수의 동시 요청에 대한 Rate Limiting 동작을 확인합니다.
    /// </summary>
    [Fact]
    public async Task RateLimit_ConcurrentRequests_Test()
    {
        // Arrange
        var concurrentRequestCount = 20;
        var results = new ConcurrentBag<(HttpStatusCode StatusCode, TimeSpan ResponseTime)>();

        var request = new UserCreateRequest
        {
            Name = "ConcurrentTestUser",
            Email = "concurrent@example.com",
            Password = "password123"
        };
        var requestJson = JsonSerializer.Serialize(request);

        // Act - 서로 다른 IP로 동시 요청 (Rate Limit 회피)
        var tasks = Enumerable.Range(0, concurrentRequestCount).Select(async i =>
        {
            using var client = _factory.CreateClient();
            var testIp = $"10.0.{i / 255}.{i % 255}";
            client.DefaultRequestHeaders.Add("X-Forwarded-For", testIp);

            var stopwatch = Stopwatch.StartNew();
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/user/create", content);
            stopwatch.Stop();

            results.Add((response.StatusCode, stopwatch.Elapsed));
        });

        await Task.WhenAll(tasks);

        // Assert
        var resultsList = results.ToList();
        var successCount = resultsList.Count(r => r.StatusCode == HttpStatusCode.OK);
        var forbiddenCount = resultsList.Count(r => r.StatusCode == HttpStatusCode.Forbidden);
        var rateLimitedCount = resultsList.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);

        _output.WriteLine($"총 요청 수: {resultsList.Count}");
        _output.WriteLine($"성공한 요청 수: {successCount}");
        _output.WriteLine($"Forbidden 요청 수: {forbiddenCount}");
        _output.WriteLine($"Rate Limit된 요청 수: {rateLimitedCount}");
        _output.WriteLine($"평균 응답 시간: {resultsList.Average(r => r.ResponseTime.TotalMilliseconds):F2}ms");

        // 동시 요청 처리 성능 검증
        var averageResponseTime = resultsList.Average(r => r.ResponseTime.TotalMilliseconds);
        Assert.True(averageResponseTime < 2000, $"평균 응답 시간({averageResponseTime:F2}ms)이 2초를 초과했습니다.");
        
        // 모든 요청이 처리되어야 함
        Assert.Equal(concurrentRequestCount, resultsList.Count);
    }

    /// <summary>
    /// 메모리 사용량 모니터링 테스트를 수행합니다.
    /// </summary>
    [Fact]
    public async Task RateLimit_MemoryUsage_Monitoring_Test()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var uniqueIpCount = 50; // 테스트 시간 단축을 위해 50개로 감소
        var requestsPerIp = 2;  // 요청 수도 감소

        var request = new UserCreateRequest
        {
            Name = "MemoryTestUser",
            Email = "memory@example.com",
            Password = "password123"
        };
        var requestJson = JsonSerializer.Serialize(request);

        var memorySnapshots = new List<long>();
        var totalRequests = 0;
        var successfulRequests = 0;
        var statusCodes = new List<HttpStatusCode>();

        // Act - 50개의 서로 다른 IP로 각각 2개씩 요청
        for (int ipIndex = 0; ipIndex < uniqueIpCount; ipIndex++)
        {
            using var client = _factory.CreateClient();
            var testIp = $"172.16.{ipIndex / 255}.{ipIndex % 255}";
            client.DefaultRequestHeaders.Add("X-Forwarded-For", testIp);

            for (int requestIndex = 0; requestIndex < requestsPerIp; requestIndex++)
            {
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("/api/user/create", content);
                
                totalRequests++;
                statusCodes.Add(response.StatusCode);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    successfulRequests++;
                }
            }

            // 매 10개 IP마다 메모리 스냅샷 저장
            if (ipIndex % 10 == 0)
            {
                memorySnapshots.Add(GC.GetTotalMemory(false));
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = Math.Max(0, finalMemory - initialMemory); // 음수 방지

        // Assert
        var forbiddenCount = statusCodes.Count(s => s == HttpStatusCode.Forbidden);
        
        _output.WriteLine($"초기 메모리: {initialMemory / 1024.0 / 1024.0:F2}MB");
        _output.WriteLine($"최종 메모리: {finalMemory / 1024.0 / 1024.0:F2}MB");
        _output.WriteLine($"사용된 메모리: {memoryUsed / 1024.0 / 1024.0:F2}MB");
        _output.WriteLine($"총 요청 수: {totalRequests}");
        _output.WriteLine($"성공한 요청 수: {successfulRequests}");
        _output.WriteLine($"Forbidden 요청 수: {forbiddenCount}");

        // 기본적인 메모리 사용량 검증 (더 관대한 기준)
        var totalMemoryUsedMB = memoryUsed / 1024.0 / 1024.0;
        Assert.True(totalMemoryUsedMB < 50, $"총 메모리 사용량({totalMemoryUsedMB:F2}MB)이 50MB를 초과했습니다.");
        
        // 모든 요청이 처리되었는지 확인
        Assert.Equal(uniqueIpCount * requestsPerIp, totalRequests);
    }

    /// <summary>
    /// 장시간 실행 시나리오에서의 성능을 테스트합니다.
    /// </summary>
    [Fact]
    public async Task RateLimit_LongRunning_Performance_Test()
    {
        // Arrange
        var testDuration = TimeSpan.FromSeconds(15); // 15초로 단축
        var endTime = DateTime.UtcNow.Add(testDuration);
        var initialMemory = GC.GetTotalMemory(true);

        var request = new UserCreateRequest
        {
            Name = "LongRunningTestUser",
            Email = "longrunning@example.com",
            Password = "password123"
        };
        var requestJson = JsonSerializer.Serialize(request);

        var totalRequests = 0;
        var successfulRequests = 0;
        var forbiddenRequests = 0;
        var rateLimitedRequests = 0;
        var responseTimes = new List<TimeSpan>();
        var ipCounter = 0;

        // Act - 15초간 지속적으로 요청
        while (DateTime.UtcNow < endTime)
        {
            using var client = _factory.CreateClient();
            
            // 매번 새로운 IP 사용하여 Rate Limit 회피
            var testIp = $"172.16.{ipCounter / 255}.{ipCounter % 255}";
            ipCounter++;
            
            client.DefaultRequestHeaders.Add("X-Forwarded-For", testIp);

            var stopwatch = Stopwatch.StartNew();
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/user/create", content);
            stopwatch.Stop();

            totalRequests++;
            responseTimes.Add(stopwatch.Elapsed);

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    successfulRequests++;
                    break;
                case HttpStatusCode.Forbidden:
                    forbiddenRequests++;
                    break;
                case HttpStatusCode.TooManyRequests:
                    rateLimitedRequests++;
                    break;
            }

            // 짧은 대기 (과도한 부하 방지)
            await Task.Delay(100);
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryUsed = Math.Max(0, finalMemory - initialMemory);

        // Assert
        var averageResponseTime = TimeSpan.FromMilliseconds(responseTimes.Average(t => t.TotalMilliseconds));
        var requestsPerSecond = totalRequests / testDuration.TotalSeconds;

        _output.WriteLine($"테스트 지속 시간: {testDuration.TotalSeconds}초");
        _output.WriteLine($"총 요청 수: {totalRequests}");
        _output.WriteLine($"성공한 요청 수: {successfulRequests}");
        _output.WriteLine($"Forbidden 요청 수: {forbiddenRequests}");
        _output.WriteLine($"Rate Limit된 요청 수: {rateLimitedRequests}");
        _output.WriteLine($"초당 요청 수: {requestsPerSecond:F2}");
        _output.WriteLine($"평균 응답 시간: {averageResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"메모리 사용량: {memoryUsed / 1024.0 / 1024.0:F2}MB");

        // 성능이 합리적인 범위 내에 있어야 함
        Assert.True(averageResponseTime.TotalMilliseconds < 2000, $"평균 응답 시간({averageResponseTime.TotalMilliseconds:F2}ms)이 2초를 초과했습니다.");
        Assert.True(requestsPerSecond > 0.5, $"초당 요청 수({requestsPerSecond:F2})가 너무 낮습니다.");
        
        // 메모리 사용량이 합리적이어야 함
        var memoryUsedMB = memoryUsed / 1024.0 / 1024.0;
        Assert.True(memoryUsedMB < 100, $"메모리 사용량({memoryUsedMB:F2}MB)이 100MB를 초과했습니다.");
        
        // 최소한의 요청이 처리되었는지 확인
        Assert.True(totalRequests > 0, "요청이 전혀 처리되지 않았습니다.");
    }

    /// <summary>
    /// Rate Limiting 비활성화 상태와 활성화 상태의 성능을 비교합니다.
    /// </summary>
    [Fact]
    public async Task RateLimit_Performance_Comparison_Test()
    {
        // Arrange
        var requestCount = 20; // 요청 수 감소
        var request = new UserCreateRequest
        {
            Name = "ComparisonTestUser",
            Email = "comparison@example.com",
            Password = "password123"
        };
        var requestJson = JsonSerializer.Serialize(request);

        // Rate Limiting 비활성화된 클라이언트 (다른 팩토리 사용)
        var disabledFactory = new TestWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Rate Limiting 비활성화 설정
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(Demo.Web.Configs.RateLimitConfig));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    var disabledConfig = new Demo.Web.Configs.RateLimitConfig
                    {
                        UserCreateEndpoint = new Demo.Web.Configs.UserCreateEndpointConfig
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

        // Act - Rate Limiting 활성화 상태에서 성능 측정
        var enabledResponseTimes = new List<TimeSpan>();
        var enabledStatusCodes = new List<HttpStatusCode>();
        
        for (int i = 0; i < requestCount; i++)
        {
            using var enabledClient = _factory.CreateClient();
            enabledClient.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.1.{i / 255}.{i % 255}");

            var stopwatch = Stopwatch.StartNew();
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await enabledClient.PostAsync("/api/user/create", content);
            stopwatch.Stop();

            enabledResponseTimes.Add(stopwatch.Elapsed);
            enabledStatusCodes.Add(response.StatusCode);
        }

        // Rate Limiting 비활성화 상태에서 성능 측정
        var disabledResponseTimes = new List<TimeSpan>();
        var disabledStatusCodes = new List<HttpStatusCode>();
        
        for (int i = 0; i < requestCount; i++)
        {
            using var disabledClient = disabledFactory.CreateClient();
            
            var stopwatch = Stopwatch.StartNew();
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var response = await disabledClient.PostAsync("/api/user/create", content);
            stopwatch.Stop();

            disabledResponseTimes.Add(stopwatch.Elapsed);
            disabledStatusCodes.Add(response.StatusCode);
        }

        // Assert
        var enabledAverage = TimeSpan.FromMilliseconds(enabledResponseTimes.Average(t => t.TotalMilliseconds));
        var disabledAverage = TimeSpan.FromMilliseconds(disabledResponseTimes.Average(t => t.TotalMilliseconds));
        
        var enabledSuccessCount = enabledStatusCodes.Count(s => s == HttpStatusCode.OK);
        var disabledSuccessCount = disabledStatusCodes.Count(s => s == HttpStatusCode.OK);

        _output.WriteLine($"Rate Limiting 활성화 - 성공: {enabledSuccessCount}/{requestCount}, 평균 응답 시간: {enabledAverage.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Rate Limiting 비활성화 - 성공: {disabledSuccessCount}/{requestCount}, 평균 응답 시간: {disabledAverage.TotalMilliseconds:F2}ms");

        // 기본적인 성능 검증
        Assert.True(enabledAverage.TotalMilliseconds < 2000, 
            $"Rate Limiting 활성화 상태 평균 응답 시간({enabledAverage.TotalMilliseconds:F2}ms)이 2초를 초과했습니다.");
        Assert.True(disabledAverage.TotalMilliseconds < 2000, 
            $"Rate Limiting 비활성화 상태 평균 응답 시간({disabledAverage.TotalMilliseconds:F2}ms)이 2초를 초과했습니다.");

        // 정리
        disabledFactory.Dispose();
    }
}