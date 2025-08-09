using System.Diagnostics;
using System.Net;

namespace Demo.Web.IntegrationTests;

/// <summary>
/// OpenTelemetry 성능 영향에 대한 벤치마크 테스트
/// </summary>
public class OpenTelemetryPerformanceTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OpenTelemetryPerformanceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ApplicationStartup_ShouldNotExceedPerformanceThreshold()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act - 애플리케이션이 이미 시작되었으므로 첫 번째 요청으로 측정
        var response = await _client.GetAsync("/test/logging");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // 첫 번째 요청 응답 시간이 5초 이내여야 함 (애플리케이션 초기화 포함)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, 
            "애플리케이션 시작 시간이 성능 임계값을 초과했습니다");
        
        // 텔레메트리 데이터가 수집되었는지 확인
        _factory.ExportedActivities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task HttpRequestProcessing_ShouldHaveMinimalOverhead()
    {
        // Arrange
        const int requestCount = 10;
        var responseTimes = new List<long>();

        // Warm-up 요청
        await _client.GetAsync("/test/logging");
        _factory.ClearExportedData();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync("/test/logging");
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();

        // 평균 응답 시간이 500ms 이내여야 함
        averageResponseTime.Should().BeLessThan(500, 
            $"평균 응답 시간이 임계값을 초과했습니다. 평균: {averageResponseTime}ms");

        // 최대 응답 시간이 1초 이내여야 함
        maxResponseTime.Should().BeLessThan(1000, 
            $"최대 응답 시간이 임계값을 초과했습니다. 최대: {maxResponseTime}ms");

        // 모든 요청에 대한 트레이스가 생성되었는지 확인
        var httpActivities = _factory.ExportedActivities
            .Where(a => a.Source.Name == "Microsoft.AspNetCore")
            .ToList();

        httpActivities.Should().HaveCount(requestCount, 
            "모든 HTTP 요청에 대한 트레이스가 생성되어야 합니다");
    }

    [Fact]
    public async Task MemoryUsage_ShouldBeWithinAcceptableLimits()
    {
        // Arrange
        const int requestCount = 50;
        var initialMemory = GC.GetTotalMemory(true);

        // Act - 여러 요청을 보내서 메모리 사용량 측정
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < requestCount; i++)
        {
            tasks.Add(_client.GetAsync("/test/logging"));
        }

        var responses = await Task.WhenAll(tasks);

        // 가비지 컬렉션 강제 실행
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.NoContent));

        // 메모리 증가량이 50MB 이내여야 함
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);
        memoryIncreaseMB.Should().BeLessThan(50, 
            $"메모리 사용량 증가가 임계값을 초과했습니다. 증가량: {memoryIncreaseMB:F2}MB");

        // 텔레메트리 데이터가 수집되었는지 확인
        _factory.ExportedActivities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldMaintainPerformance()
    {
        // Arrange
        const int concurrentRequests = 20;
        const int maxAcceptableResponseTime = 2000; // 2초

        _factory.ClearExportedData();

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        var tasks = new List<Task<HttpResponseMessage>>();
        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(_client.GetAsync("/test/logging"));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.NoContent));

        // 전체 동시 요청 처리 시간이 임계값 이내여야 함
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(maxAcceptableResponseTime,
            $"동시 요청 처리 시간이 임계값을 초과했습니다. 처리 시간: {stopwatch.ElapsedMilliseconds}ms");

        // 모든 요청에 대한 트레이스가 생성되었는지 확인
        var httpActivities = _factory.ExportedActivities
            .Where(a => a.Source.Name == "Microsoft.AspNetCore")
            .ToList();

        httpActivities.Should().HaveCount(concurrentRequests,
            "모든 동시 요청에 대한 트레이스가 생성되어야 합니다");

        // 각 트레이스가 고유한 TraceId를 가져야 함
        var uniqueTraceIds = httpActivities.Select(a => a.TraceId).Distinct().Count();
        uniqueTraceIds.Should().Be(concurrentRequests,
            "각 요청은 고유한 TraceId를 가져야 합니다");
    }

    [Fact]
    public async Task MetricsCollection_ShouldNotImpactPerformance()
    {
        // Arrange
        const int requestCount = 30;
        var responseTimes = new List<long>();

        // Warm-up
        await _client.GetAsync("/test/logging");
        _factory.ClearExportedData();

        // Act
        for (int i = 0; i < requestCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync("/test/logging");
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            responseTimes.Add(stopwatch.ElapsedMilliseconds);

            // 메트릭 수집을 위한 짧은 대기
            await Task.Delay(10);
        }

        // Assert
        var averageResponseTime = responseTimes.Average();
        var p95ResponseTime = responseTimes.OrderBy(x => x).Skip((int)(requestCount * 0.95)).First();

        // 평균 응답 시간이 300ms 이내여야 함
        averageResponseTime.Should().BeLessThan(300,
            $"메트릭 수집으로 인한 평균 응답 시간이 임계값을 초과했습니다. 평균: {averageResponseTime}ms");

        // 95퍼센타일 응답 시간이 800ms 이내여야 함
        p95ResponseTime.Should().BeLessThan(800,
            $"메트릭 수집으로 인한 P95 응답 시간이 임계값을 초과했습니다. P95: {p95ResponseTime}ms");

        // 메트릭이 수집되었는지 확인
        await Task.Delay(200); // 메트릭 수집 대기
        _factory.ExportedMetrics.Should().NotBeEmpty("메트릭이 수집되어야 합니다");
    }

    [Fact]
    public async Task TraceExport_ShouldNotBlockRequestProcessing()
    {
        // Arrange
        const int requestCount = 25;
        var allResponseTimes = new List<long>();

        _factory.ClearExportedData();

        // Act - 연속적인 요청으로 트레이스 내보내기 부하 생성
        for (int i = 0; i < requestCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync("/test/logging");
            stopwatch.Stop();

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            allResponseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var firstHalfAverage = allResponseTimes.Take(requestCount / 2).Average();
        var secondHalfAverage = allResponseTimes.Skip(requestCount / 2).Average();

        // 후반부 요청의 평균 응답 시간이 전반부보다 50% 이상 증가하지 않아야 함
        var performanceDegradation = (secondHalfAverage - firstHalfAverage) / firstHalfAverage;
        performanceDegradation.Should().BeLessThan(0.5,
            $"트레이스 내보내기로 인한 성능 저하가 임계값을 초과했습니다. " +
            $"전반부 평균: {firstHalfAverage:F2}ms, 후반부 평균: {secondHalfAverage:F2}ms, " +
            $"성능 저하: {performanceDegradation:P2}");

        // 모든 트레이스가 수집되었는지 확인
        _factory.ExportedActivities.Should().NotBeEmpty("트레이스가 수집되어야 합니다");
    }

    [Fact]
    public async Task ResourceUtilization_ShouldBeEfficient()
    {
        // Arrange
        const int loadTestDuration = 5000; // 5초
        var requestCount = 0;
        var errorCount = 0;
        var responseTimes = new List<long>();

        var cancellationTokenSource = new CancellationTokenSource(loadTestDuration);
        _factory.ClearExportedData();

        // Act - 지속적인 부하 테스트
        var loadTestTasks = new List<Task>();
        
        // 동시에 5개의 요청 스레드 실행
        for (int i = 0; i < 5; i++)
        {
            loadTestTasks.Add(Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        var response = await _client.GetAsync("/test/logging", cancellationTokenSource.Token);
                        stopwatch.Stop();

                        if (response.StatusCode == HttpStatusCode.NoContent)
                        {
                            Interlocked.Increment(ref requestCount);
                            lock (responseTimes)
                            {
                                responseTimes.Add(stopwatch.ElapsedMilliseconds);
                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref errorCount);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        Interlocked.Increment(ref errorCount);
                    }

                    // 짧은 대기로 CPU 사용률 조절
                    await Task.Delay(10, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token));
        }

        await Task.WhenAll(loadTestTasks);

        // Assert
        requestCount.Should().BeGreaterThan(100, "충분한 수의 요청이 처리되어야 합니다");
        
        var errorRate = (double)errorCount / (requestCount + errorCount);
        errorRate.Should().BeLessThan(0.01, $"에러율이 1%를 초과했습니다. 에러율: {errorRate:P2}");

        if (responseTimes.Any())
        {
            var averageResponseTime = responseTimes.Average();
            averageResponseTime.Should().BeLessThan(1000,
                $"부하 테스트 중 평균 응답 시간이 임계값을 초과했습니다. 평균: {averageResponseTime:F2}ms");
        }

        // 텔레메트리 데이터가 수집되었는지 확인
        _factory.ExportedActivities.Should().NotBeEmpty("부하 테스트 중에도 트레이스가 수집되어야 합니다");
    }
}