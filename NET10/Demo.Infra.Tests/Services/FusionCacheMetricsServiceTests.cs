using Demo.Infra.Configs;
using Demo.Infra.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Demo.Infra.Tests.Services;

/// <summary>
/// FusionCacheMetricsService에 대한 단위 테스트
/// 요구사항 4.2, 4.3 검증
/// </summary>
public class FusionCacheMetricsServiceTests : IDisposable
{
    private readonly Mock<ILogger<FusionCacheMetricsService>> _mockLogger;
    private readonly IOptions<FusionCacheConfig> _config;
    private readonly FusionCacheMetricsService _metricsService;

    public FusionCacheMetricsServiceTests()
    {
        _mockLogger = new Mock<ILogger<FusionCacheMetricsService>>();
        
        // Mock 로거가 Debug 레벨을 활성화하도록 설정
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(true);
        
        _config = Options.Create(new FusionCacheConfig
        {
            EnableMetrics = true,
            EnableDetailedLogging = true
        });
        
        _metricsService = new FusionCacheMetricsService(_mockLogger.Object, _config);
    }

    /// <summary>
    /// 캐시 히트 메트릭이 올바르게 기록되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void RecordCacheOperation_CacheHit_ShouldUpdateHitMetrics()
    {
        // Arrange
        const string cacheName = "TestCache";
        const string operation = "get";
        const string result = "hit";
        const double durationMs = 10.5;

        // Act
        _metricsService.RecordCacheOperation(cacheName, operation, result, durationMs);

        // Assert
        var metrics = _metricsService.GetCacheMetrics(cacheName);
        Assert.NotNull(metrics);
        Assert.Equal(1, metrics.TotalHits);
        Assert.Equal(0, metrics.TotalMisses);
        Assert.Equal(0, metrics.TotalErrors);
        Assert.Equal(100.0, metrics.HitRatePercent);
        Assert.Equal(0.0, metrics.MissRatePercent);
        Assert.Equal(durationMs, metrics.AverageResponseTimeMs);
    }

    /// <summary>
    /// 캐시 미스 메트릭이 올바르게 기록되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void RecordCacheOperation_CacheMiss_ShouldUpdateMissMetrics()
    {
        // Arrange
        const string cacheName = "TestCache";
        const string operation = "get";
        const string result = "miss";
        const double durationMs = 15.2;

        // Act
        _metricsService.RecordCacheOperation(cacheName, operation, result, durationMs);

        // Assert
        var metrics = _metricsService.GetCacheMetrics(cacheName);
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalHits);
        Assert.Equal(1, metrics.TotalMisses);
        Assert.Equal(0, metrics.TotalErrors);
        Assert.Equal(0.0, metrics.HitRatePercent);
        Assert.Equal(100.0, metrics.MissRatePercent);
        Assert.Equal(durationMs, metrics.AverageResponseTimeMs);
    }

    /// <summary>
    /// 캐시 설정 메트릭이 올바르게 기록되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void RecordCacheOperation_CacheSet_ShouldUpdateSetMetrics()
    {
        // Arrange
        const string cacheName = "TestCache";
        const string operation = "set";
        const string result = "success";
        const double durationMs = 5.8;

        // Act
        _metricsService.RecordCacheOperation(cacheName, operation, result, durationMs);

        // Assert
        var metrics = _metricsService.GetCacheMetrics(cacheName);
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalHits);
        Assert.Equal(0, metrics.TotalMisses);
        Assert.Equal(1, metrics.TotalSets);
        Assert.Equal(0, metrics.TotalErrors);
    }

    /// <summary>
    /// 캐시 오류 메트릭이 올바르게 기록되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void RecordCacheOperation_CacheError_ShouldUpdateErrorMetrics()
    {
        // Arrange
        const string cacheName = "TestCache";
        const string operation = "get";
        const string result = "error";
        const double durationMs = 100.0;

        // Act
        _metricsService.RecordCacheOperation(cacheName, operation, result, durationMs);

        // Assert
        var metrics = _metricsService.GetCacheMetrics(cacheName);
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.TotalHits);
        Assert.Equal(0, metrics.TotalMisses);
        Assert.Equal(0, metrics.TotalSets);
        Assert.Equal(1, metrics.TotalErrors);
    }

    /// <summary>
    /// 히트율과 미스율이 올바르게 계산되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void RecordCacheOperation_MultipleOperations_ShouldCalculateRatesCorrectly()
    {
        // Arrange
        const string cacheName = "TestCache";

        // Act - 7번 히트, 3번 미스 기록
        for (int i = 0; i < 7; i++)
        {
            _metricsService.RecordCacheOperation(cacheName, "get", "hit", 10.0);
        }
        
        for (int i = 0; i < 3; i++)
        {
            _metricsService.RecordCacheOperation(cacheName, "get", "miss", 20.0);
        }

        // Assert
        var metrics = _metricsService.GetCacheMetrics(cacheName);
        Assert.NotNull(metrics);
        Assert.Equal(7, metrics.TotalHits);
        Assert.Equal(3, metrics.TotalMisses);
        Assert.Equal(70.0, metrics.HitRatePercent);
        Assert.Equal(30.0, metrics.MissRatePercent);
        
        // 평균 응답 시간 검증: (7 * 10.0 + 3 * 20.0) / 10 = 13.0
        Assert.Equal(13.0, metrics.AverageResponseTimeMs);
    }

    /// <summary>
    /// 집계된 메트릭이 올바르게 계산되는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void GetAggregatedMetrics_MultipleCaches_ShouldAggregateCorrectly()
    {
        // Arrange
        const string cache1 = "Cache1";
        const string cache2 = "Cache2";

        // Act
        // Cache1: 5 hits, 2 misses
        for (int i = 0; i < 5; i++)
        {
            _metricsService.RecordCacheOperation(cache1, "get", "hit", 10.0);
        }
        for (int i = 0; i < 2; i++)
        {
            _metricsService.RecordCacheOperation(cache1, "get", "miss", 15.0);
        }

        // Cache2: 3 hits, 4 misses
        for (int i = 0; i < 3; i++)
        {
            _metricsService.RecordCacheOperation(cache2, "get", "hit", 20.0);
        }
        for (int i = 0; i < 4; i++)
        {
            _metricsService.RecordCacheOperation(cache2, "get", "miss", 25.0);
        }

        // Assert
        var aggregated = _metricsService.GetAggregatedMetrics();
        Assert.Equal(8, aggregated.TotalHits); // 5 + 3
        Assert.Equal(6, aggregated.TotalMisses); // 2 + 4
        
        // 전체 히트율: 8 / (8 + 6) * 100 = 57.14%
        Assert.Equal(57.14, aggregated.HitRatePercent, 2);
        
        // 전체 미스율: 6 / (8 + 6) * 100 = 42.86%
        Assert.Equal(42.86, aggregated.MissRatePercent, 2);
    }

    /// <summary>
    /// 메트릭 초기화가 올바르게 작동하는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void ResetMetrics_SpecificCache_ShouldResetOnlyThatCache()
    {
        // Arrange
        const string cache1 = "Cache1";
        const string cache2 = "Cache2";

        _metricsService.RecordCacheOperation(cache1, "get", "hit", 10.0);
        _metricsService.RecordCacheOperation(cache2, "get", "hit", 10.0);

        // Act
        _metricsService.ResetMetrics(cache1);

        // Assert
        var cache1Metrics = _metricsService.GetCacheMetrics(cache1);
        var cache2Metrics = _metricsService.GetCacheMetrics(cache2);

        Assert.NotNull(cache1Metrics);
        Assert.NotNull(cache2Metrics);
        
        Assert.Equal(0, cache1Metrics.TotalHits); // 초기화됨
        Assert.Equal(1, cache2Metrics.TotalHits); // 유지됨
    }

    /// <summary>
    /// 모든 메트릭 초기화가 올바르게 작동하는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void ResetMetrics_AllCaches_ShouldResetAllMetrics()
    {
        // Arrange
        const string cache1 = "Cache1";
        const string cache2 = "Cache2";

        _metricsService.RecordCacheOperation(cache1, "get", "hit", 10.0);
        _metricsService.RecordCacheOperation(cache2, "get", "hit", 10.0);

        // Act
        _metricsService.ResetMetrics();

        // Assert
        var cache1Metrics = _metricsService.GetCacheMetrics(cache1);
        var cache2Metrics = _metricsService.GetCacheMetrics(cache2);

        Assert.NotNull(cache1Metrics);
        Assert.NotNull(cache2Metrics);
        
        Assert.Equal(0, cache1Metrics.TotalHits);
        Assert.Equal(0, cache2Metrics.TotalHits);
    }

    /// <summary>
    /// 메트릭 요약 로깅이 올바르게 수행되는지 검증
    /// 요구사항 4.2 검증
    /// </summary>
    [Fact]
    public void LogMetricsSummary_WithMetrics_ShouldLogSummaryInformation()
    {
        // Arrange
        const string cacheName = "TestCache";
        
        _metricsService.RecordCacheOperation(cacheName, "get", "hit", 10.0);
        _metricsService.RecordCacheOperation(cacheName, "get", "miss", 15.0);
        _metricsService.RecordCacheOperation(cacheName, "set", "success", 5.0);

        // Act
        _metricsService.LogMetricsSummary();

        // Assert
        // 요약 로그가 Information 레벨로 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("FusionCache 메트릭 요약") &&
                    v.ToString()!.Contains("총 작업") &&
                    v.ToString()!.Contains("히트") &&
                    v.ToString()!.Contains("미스") &&
                    v.ToString()!.Contains("히트율") &&
                    v.ToString()!.Contains("미스율")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // 상세 로깅이 활성화된 경우 캐시별 메트릭도 로깅되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("캐시별 메트릭") &&
                    v.ToString()!.Contains(cacheName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// 메트릭이 비활성화된 경우 기록되지 않는지 검증
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public void RecordCacheOperation_WithMetricsDisabled_ShouldNotRecordMetrics()
    {
        // Arrange
        var configWithDisabledMetrics = Options.Create(new FusionCacheConfig
        {
            EnableMetrics = false
        });
        
        using var metricsServiceDisabled = new FusionCacheMetricsService(_mockLogger.Object, configWithDisabledMetrics);

        // Act
        metricsServiceDisabled.RecordCacheOperation("TestCache", "get", "hit", 10.0);

        // Assert
        var metrics = metricsServiceDisabled.GetCacheMetrics("TestCache");
        Assert.Null(metrics); // 메트릭이 비활성화되어 있으므로 null이어야 함
    }

    /// <summary>
    /// 추가 태그가 올바르게 처리되는지 검증
    /// 요구사항 4.2 검증 (구조화된 로깅)
    /// </summary>
    [Fact]
    public void RecordCacheOperation_WithAdditionalTags_ShouldLogTags()
    {
        // Arrange
        const string cacheName = "TestCache";
        var additionalTags = new Dictionary<string, object?>
        {
            ["key_prefix"] = "test",
            ["client_ip_hash"] = 12345,
            ["country_code"] = "KR"
        };

        // Act
        _metricsService.RecordCacheOperation(cacheName, "get", "hit", 10.0, additionalTags);

        // Assert
        // 상세 로깅이 활성화된 경우 태그 정보가 로깅되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("캐시 메트릭 기록") &&
                    v.ToString()!.Contains("Tags=")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public void Dispose()
    {
        _metricsService?.Dispose();
        GC.SuppressFinalize(this);
    }
}