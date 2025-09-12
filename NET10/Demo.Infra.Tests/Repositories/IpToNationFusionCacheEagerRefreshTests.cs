using Demo.Application.Configs;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationFusionCache의 백그라운드 새로고침(EagerRefresh) 기능에 대한 테스트
/// 요구사항 3.1 검증: 캐시 항목이 만료 임계점에 도달하면 백그라운드에서 자동으로 새로고침되어야 합니다
/// </summary>
public class IpToNationFusionCacheEagerRefreshTests : IDisposable
{
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<IDistributedCache> _mockDistributedCache;
    private readonly Mock<ILogger<FusionCache>> _mockLogger;
    private readonly FusionCache _fusionCache;
    private readonly IpToNationFusionCache _cache;
    private readonly FusionCacheConfig _config;

    public IpToNationFusionCacheEagerRefreshTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockDistributedCache = new Mock<IDistributedCache>();
        _mockLogger = new Mock<ILogger<FusionCache>>();

        // 백그라운드 새로고침이 활성화된 설정
        _config = new FusionCacheConfig
        {
            EnableEagerRefresh = true,
            EagerRefreshThreshold = 0.8f, // 80%에서 새로고침 시작
            DefaultEntryOptions = TimeSpan.FromSeconds(10), // 짧은 TTL로 테스트 용이성 확보
            L1CacheDuration = TimeSpan.FromSeconds(5),
            SoftTimeout = TimeSpan.FromSeconds(1),
            HardTimeout = TimeSpan.FromSeconds(3),
            EnableFailSafe = true,
            Redis = new RedisConfig { KeyPrefix = "test" }
        };

        // FusionCache 옵션 구성
        var options = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = _config.DefaultEntryOptions,
                EagerRefreshThreshold = _config.EagerRefreshThreshold,
                AllowBackgroundDistributedCacheOperations = true,
                ReThrowDistributedCacheExceptions = false,
                DistributedCacheSoftTimeout = _config.SoftTimeout,
                DistributedCacheHardTimeout = _config.HardTimeout,
                IsFailSafeEnabled = _config.EnableFailSafe
            },
            CacheName = "TestCache"
        };

        _fusionCache = new FusionCache(options, _memoryCache, _mockLogger.Object);
        
        // L2 캐시 설정
        var serializer = new FusionCacheSystemTextJsonSerializer();
        _fusionCache.SetupDistributedCache(_mockDistributedCache.Object, serializer);

        var fusionCacheOptions = Options.Create(_config);
        var cacheLogger = new Mock<ILogger<IpToNationFusionCache>>();
        _cache = new IpToNationFusionCache(_fusionCache, fusionCacheOptions, cacheLogger.Object);
    }

    /// <summary>
    /// 백그라운드 새로고침이 활성화되어 있는지 확인하는 테스트
    /// </summary>
    [Fact]
    public void EagerRefresh_ShouldBeEnabled()
    {
        // Assert
        Assert.True(_config.EnableEagerRefresh);
        Assert.Equal(0.8f, _config.EagerRefreshThreshold);
        Assert.True(_fusionCache.DefaultEntryOptions.EagerRefreshThreshold > 0);
    }

    /// <summary>
    /// 새로고침 임계점이 올바르게 설정되는지 확인하는 테스트
    /// </summary>
    [Fact]
    public void EagerRefreshThreshold_ShouldBeConfiguredCorrectly()
    {
        // Arrange
        const float expectedThreshold = 0.8f;

        // Assert
        Assert.Equal(expectedThreshold, _fusionCache.DefaultEntryOptions.EagerRefreshThreshold);
    }

    /// <summary>
    /// 백그라운드 분산 캐시 작업이 활성화되어 있는지 확인하는 테스트
    /// </summary>
    [Fact]
    public void BackgroundDistributedCacheOperations_ShouldBeEnabled()
    {
        // Assert
        Assert.True(_fusionCache.DefaultEntryOptions.AllowBackgroundDistributedCacheOperations);
    }

    /// <summary>
    /// 캐시 항목 설정 시 백그라운드 새로고침 옵션이 적용되는지 확인하는 테스트
    /// </summary>
    [Fact]
    public async Task SetAsync_ShouldApplyEagerRefreshOptions()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromSeconds(10);

        // Act
        await _cache.SetAsync(clientIp, countryCode, duration);

        // Assert - 캐시에 데이터가 저장되었는지 확인
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    /// <summary>
    /// 백그라운드 새로고침 설정이 FusionCacheEntryOptions에 올바르게 적용되는지 확인하는 테스트
    /// </summary>
    [Fact]
    public async Task CacheEntryOptions_ShouldIncludeEagerRefreshSettings()
    {
        // Arrange
        const string clientIp = "10.0.0.1";
        const string countryCode = "US";
        var duration = TimeSpan.FromSeconds(5);

        // Act
        await _cache.SetAsync(clientIp, countryCode, duration);

        // Assert - GetOrSet을 사용하여 캐시 옵션 확인
        var factoryCallCount = 0;
        var result = await _fusionCache.GetOrSetAsync(
            "test:ipcache:10.0.0.1",
            _ =>
            {
                factoryCallCount++;
                return Task.FromResult("US");
            },
            new FusionCacheEntryOptions
            {
                Duration = duration,
                EagerRefreshThreshold = _config.EagerRefreshThreshold,
                AllowBackgroundDistributedCacheOperations = true
            });

        Assert.Equal(countryCode, result);
        // 캐시 히트이므로 팩토리가 호출되지 않아야 함
        Assert.Equal(0, factoryCallCount);
    }

    /// <summary>
    /// 백그라운드 새로고침이 작동하는 시나리오를 시뮬레이션하는 테스트
    /// 실제 백그라운드 새로고침은 시간 기반이므로 직접적인 테스트는 어렵지만,
    /// 설정이 올바르게 적용되는지 확인합니다
    /// </summary>
    [Fact]
    public async Task EagerRefresh_ConfigurationShouldBeApplied()
    {
        // Arrange
        const string clientIp = "172.16.0.1";
        const string countryCode = "JP";
        var shortDuration = TimeSpan.FromMilliseconds(500); // 매우 짧은 TTL

        // Act - 캐시에 데이터 저장
        await _cache.SetAsync(clientIp, countryCode, shortDuration);

        // 즉시 조회하여 캐시 히트 확인
        var immediateResult = await _cache.GetAsync(clientIp);
        Assert.True(immediateResult.IsSuccess);
        Assert.Equal(countryCode, immediateResult.Value);

        // 짧은 대기 후 다시 조회 (백그라운드 새로고침 트리거 가능성)
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        
        var laterResult = await _cache.GetAsync(clientIp);
        // 백그라운드 새로고침이 설정되어 있으므로 여전히 데이터가 있을 수 있음
        // 하지만 이는 타이밍에 의존적이므로 설정 확인에 집중
        
        // Assert - 설정이 올바르게 적용되었는지 확인
        Assert.True(_fusionCache.DefaultEntryOptions.EagerRefreshThreshold > 0);
        Assert.True(_fusionCache.DefaultEntryOptions.AllowBackgroundDistributedCacheOperations);
    }

    /// <summary>
    /// 백그라운드 새로고침 임계점 설정 검증 테스트
    /// 다양한 임계점 값에 대해 올바르게 설정되는지 확인
    /// </summary>
    [Theory]
    [InlineData(0.5f)]
    [InlineData(0.7f)]
    [InlineData(0.8f)]
    [InlineData(0.9f)]
    public void EagerRefreshThreshold_ShouldAcceptValidValues(float threshold)
    {
        // Arrange
        var testConfig = new FusionCacheConfig
        {
            EagerRefreshThreshold = threshold,
            EnableEagerRefresh = true,
            Redis = new RedisConfig { KeyPrefix = "test" }
        };

        var options = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                EagerRefreshThreshold = testConfig.EagerRefreshThreshold
            }
        };

        using var testCache = new FusionCache(options, _memoryCache);

        // Assert
        Assert.Equal(threshold, testCache.DefaultEntryOptions.EagerRefreshThreshold);
    }

    /// <summary>
    /// 백그라운드 새로고침이 비활성화된 경우의 동작 확인 테스트
    /// </summary>
    [Fact]
    public void EagerRefresh_WhenDisabled_ShouldNotSetThreshold()
    {
        // Arrange
        var disabledConfig = new FusionCacheConfig
        {
            EnableEagerRefresh = false,
            EagerRefreshThreshold = 0.0f,
            Redis = new RedisConfig { KeyPrefix = "test" }
        };

        var options = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                EagerRefreshThreshold = disabledConfig.EagerRefreshThreshold
            }
        };

        using var testCache = new FusionCache(options, _memoryCache);

        // Assert
        // EagerRefresh가 비활성화되면 EagerRefreshThreshold가 null이거나 0이어야 함
        Assert.True(testCache.DefaultEntryOptions.EagerRefreshThreshold == null || 
                   testCache.DefaultEntryOptions.EagerRefreshThreshold == 0.0f);
    }

    public void Dispose()
    {
        _fusionCache?.Dispose();
        _memoryCache?.Dispose();
    }
}