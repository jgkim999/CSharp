using Demo.Application.Configs;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationFusionCache와 FusionCacheMetricsService의 통합 테스트
/// 요구사항 4.2, 4.3 검증
/// </summary>
public class IpToNationFusionCacheMetricsIntegrationTests : IDisposable
{
    private readonly FusionCache _fusionCache;
    private readonly Mock<ILogger<IpToNationFusionCache>> _mockCacheLogger;
    private readonly Mock<ILogger<FusionCacheMetricsService>> _mockMetricsLogger;
    private readonly IOptions<FusionCacheConfig> _fusionCacheConfig;
    private readonly FusionCacheMetricsService _metricsService;
    private readonly IpToNationFusionCache _cache;

    public IpToNationFusionCacheMetricsIntegrationTests()
    {
        // 실제 FusionCache 인스턴스 생성 (메모리 캐시만 사용)
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var fusionCacheLogger = new Mock<ILogger<FusionCache>>();
        
        var options = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30)
            },
            CacheName = "IntegrationTestCache"
        };

        _fusionCache = new FusionCache(options, memoryCache, fusionCacheLogger.Object);
        
        _mockCacheLogger = new Mock<ILogger<IpToNationFusionCache>>();
        _mockMetricsLogger = new Mock<ILogger<FusionCacheMetricsService>>();
        
        _fusionCacheConfig = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "integration_test" },
            EnableDetailedLogging = true,
            EnableMetrics = true,
            CacheEventLogLevel = LogLevel.Information
        });
        
        // 메트릭 서비스 생성
        _metricsService = new FusionCacheMetricsService(_mockMetricsLogger.Object, _fusionCacheConfig);
        
        // 메트릭 서비스와 함께 캐시 생성
        _cache = new IpToNationFusionCache(_fusionCache, _fusionCacheConfig, _mockCacheLogger.Object, _metricsService);
    }

    /// <summary>
    /// 캐시 히트 시 메트릭이 올바르게 수집되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task CacheHit_ShouldCollectMetricsCorrectly()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        
        // 먼저 캐시에 데이터 설정
        await _cache.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(30));

        // Act
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
        
        // 메트릭 서비스에서 히트 메트릭 확인
        var metrics = _metricsService.GetCacheMetrics("IntegrationTestCache");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalHits);
        Assert.Equal(0, metrics.TotalMisses);
        Assert.Equal(1, metrics.TotalSets);
        Assert.Equal(100.0, metrics.HitRatePercent);
        Assert.Equal(0.0, metrics.MissRatePercent);
        Assert.True(metrics.AverageResponseTimeMs > 0);
    }

    /// <summary>
    /// 캐시 미스 시 메트릭이 올바르게 수집되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task CacheMiss_ShouldCollectMetricsCorrectly()
    {
        // Arrange
        const string clientIp = "192.168.1.999"; // 존재하지 않는 IP

        // Act
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Not found", result.Errors[0].Message);
        
        // 메트릭 서비스에서 미스 메트릭 확인
        var metrics = _metricsService.GetCacheMetrics("IntegrationTestCache");
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalHits);
        Assert.Equal(1, metrics.TotalMisses);
        Assert.Equal(0.0, metrics.HitRatePercent);
        Assert.Equal(100.0, metrics.MissRatePercent);
        Assert.True(metrics.AverageResponseTimeMs > 0);
    }

    /// <summary>
    /// 여러 캐시 작업 후 히트율과 미스율이 올바르게 계산되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task MultipleCacheOperations_ShouldCalculateRatesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            ("192.168.1.1", "KR"),
            ("192.168.1.2", "US"),
            ("192.168.1.3", "JP")
        };

        // Act
        // 데이터 설정 (3번의 set 작업)
        foreach (var (ip, country) in testData)
        {
            await _cache.SetAsync(ip, country, TimeSpan.FromMinutes(30));
        }

        // 캐시 히트 (3번)
        foreach (var (ip, expectedCountry) in testData)
        {
            var result = await _cache.GetAsync(ip);
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedCountry, result.Value);
        }

        // 캐시 미스 (2번)
        await _cache.GetAsync("192.168.1.100");
        await _cache.GetAsync("192.168.1.101");

        // Assert
        var metrics = _metricsService.GetCacheMetrics("IntegrationTestCache");
        Assert.NotNull(metrics);
        Assert.Equal(3, metrics.TotalHits);
        Assert.Equal(2, metrics.TotalMisses);
        Assert.Equal(3, metrics.TotalSets);
        Assert.Equal(0, metrics.TotalErrors);
        
        // 히트율: 3 / (3 + 2) * 100 = 60%
        Assert.Equal(60.0, metrics.HitRatePercent);
        
        // 미스율: 2 / (3 + 2) * 100 = 40%
        Assert.Equal(40.0, metrics.MissRatePercent);
        
        Assert.True(metrics.AverageResponseTimeMs > 0);
    }

    /// <summary>
    /// 집계된 메트릭이 올바르게 계산되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task AggregatedMetrics_ShouldReflectAllOperations()
    {
        // Arrange & Act
        await _cache.SetAsync("192.168.1.1", "KR", TimeSpan.FromMinutes(30));
        await _cache.GetAsync("192.168.1.1"); // 히트
        await _cache.GetAsync("192.168.1.999"); // 미스

        // Assert
        var aggregated = _metricsService.GetAggregatedMetrics();
        Assert.Equal(1, aggregated.TotalHits);
        Assert.Equal(1, aggregated.TotalMisses);
        Assert.Equal(1, aggregated.TotalSets);
        Assert.Equal(0, aggregated.TotalErrors);
        Assert.Equal(50.0, aggregated.HitRatePercent);
        Assert.Equal(50.0, aggregated.MissRatePercent);
        Assert.True(aggregated.AverageResponseTimeMs > 0);
    }

    /// <summary>
    /// 메트릭 요약 로깅이 올바르게 수행되는지 검증
    /// 요구사항 4.2 검증
    /// </summary>
    [Fact]
    public async Task LogMetricsSummary_ShouldLogCorrectInformation()
    {
        // Arrange
        await _cache.SetAsync("192.168.1.1", "KR", TimeSpan.FromMinutes(30));
        await _cache.GetAsync("192.168.1.1"); // 히트
        await _cache.GetAsync("192.168.1.999"); // 미스

        // Act
        _metricsService.LogMetricsSummary();

        // Assert
        // 메트릭 요약 로그가 기록되는지 확인
        _mockMetricsLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("FusionCache 메트릭 요약") &&
                    v.ToString()!.Contains("총 작업: 2") &&
                    v.ToString()!.Contains("히트: 1") &&
                    v.ToString()!.Contains("미스: 1") &&
                    v.ToString()!.Contains("설정: 1") &&
                    v.ToString()!.Contains("히트율: 50") &&
                    v.ToString()!.Contains("미스율: 50")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // 캐시별 상세 메트릭 로그가 기록되는지 확인 (상세 로깅 활성화됨)
        _mockMetricsLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("캐시별 메트릭") &&
                    v.ToString()!.Contains("IntegrationTestCache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// 캐시 작업 중 오류 발생 시 메트릭이 올바르게 수집되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task CacheError_ShouldCollectErrorMetrics()
    {
        // Arrange
        var mockFusionCache = new Mock<IFusionCache>();
        var testException = new InvalidOperationException("Test cache exception");
        
        // CacheName 설정
        mockFusionCache.Setup(x => x.CacheName).Returns("TestErrorCache");
        
        mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(
                It.IsAny<string>(), 
                It.IsAny<string?>(), 
                It.IsAny<FusionCacheEntryOptions?>(), 
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(testException);

        var cacheWithError = new IpToNationFusionCache(
            mockFusionCache.Object, 
            _fusionCacheConfig, 
            _mockCacheLogger.Object, 
            _metricsService);

        // Act
        var result = await cacheWithError.GetAsync("192.168.1.1");

        // Assert
        Assert.True(result.IsFailed);
        
        // 오류 메트릭이 기록되는지 확인
        // 참고: mockFusionCache는 CacheName이 null일 수 있으므로 실제 캐시 이름으로 확인
        var aggregated = _metricsService.GetAggregatedMetrics();
        Assert.True(aggregated.TotalErrors >= 1);
    }

    /// <summary>
    /// 메트릭 초기화가 올바르게 작동하는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task ResetMetrics_ShouldClearAllMetrics()
    {
        // Arrange
        await _cache.SetAsync("192.168.1.1", "KR", TimeSpan.FromMinutes(30));
        await _cache.GetAsync("192.168.1.1");

        // 메트릭이 기록되었는지 확인
        var metricsBeforeReset = _metricsService.GetCacheMetrics("IntegrationTestCache");
        Assert.NotNull(metricsBeforeReset);
        Assert.True(metricsBeforeReset.TotalHits > 0 || metricsBeforeReset.TotalSets > 0);

        // Act
        _metricsService.ResetMetrics("IntegrationTestCache");

        // Assert
        var metricsAfterReset = _metricsService.GetCacheMetrics("IntegrationTestCache");
        Assert.NotNull(metricsAfterReset);
        Assert.Equal(0, metricsAfterReset.TotalHits);
        Assert.Equal(0, metricsAfterReset.TotalMisses);
        Assert.Equal(0, metricsAfterReset.TotalSets);
        Assert.Equal(0, metricsAfterReset.TotalErrors);
        Assert.Equal(0.0, metricsAfterReset.HitRatePercent);
        Assert.Equal(0.0, metricsAfterReset.MissRatePercent);
    }

    /// <summary>
    /// 상세 로깅과 메트릭 수집이 함께 작동하는지 검증
    /// 요구사항 4.2, 4.3 검증
    /// </summary>
    [Fact]
    public async Task DetailedLoggingWithMetrics_ShouldWorkTogether()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";

        // Act
        await _cache.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(30));
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);

        // 상세 로깅이 수행되었는지 확인
        _mockCacheLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("캐시 설정 완료") &&
                    v.ToString()!.Contains("IP ") &&
                    v.ToString()!.Contains("국가 코드") &&
                    v.ToString()!.Contains("Duration")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockCacheLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("캐시 히트") &&
                    v.ToString()!.Contains("IP ") &&
                    v.ToString()!.Contains("국가 코드")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // 메트릭이 수집되었는지 확인
        var metrics = _metricsService.GetCacheMetrics("IntegrationTestCache");
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalHits);
        Assert.Equal(1, metrics.TotalSets);
    }

    public void Dispose()
    {
        _fusionCache?.Dispose();
        _metricsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}