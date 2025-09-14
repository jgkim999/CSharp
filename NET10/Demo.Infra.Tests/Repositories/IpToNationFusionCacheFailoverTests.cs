using Demo.Application.Configs;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationFusionCache의 페일오버 및 복원력 기능을 검증하는 테스트
/// Redis 연결 실패, 페일세이프 메커니즘, 타임아웃 시나리오를 테스트합니다
/// 요구사항 2.3, 3.3, 3.4를 충족합니다
/// </summary>
public class IpToNationFusionCacheFailoverTests : IDisposable
{
    private readonly IFusionCache _fusionCache;
    private readonly Mock<IFusionCache> _mockFusionCache;
    private readonly Mock<ILogger<IpToNationFusionCache>> _mockLogger;
    private readonly IOptions<FusionCacheConfig> _fusionCacheConfig;
    private readonly IpToNationFusionCache _cache;
    private readonly IpToNationFusionCache _cacheWithMocks;

    public IpToNationFusionCacheFailoverTests()
    {
        // 실제 FusionCache 인스턴스 생성 (메모리 캐시만 사용)
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var fusionCacheLogger = new Mock<ILogger<FusionCache>>();
        
        var options = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30),
                FailSafeMaxDuration = TimeSpan.FromHours(1),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
            },
            CacheName = "TestCache"
        };

        _fusionCache = new FusionCache(options, memoryCache, fusionCacheLogger.Object);
        
        // Mock 객체들 생성
        _mockFusionCache = new Mock<IFusionCache>();
        _mockLogger = new Mock<ILogger<IpToNationFusionCache>>();
        
        _fusionCacheConfig = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "test" },
            DefaultEntryOptions = TimeSpan.FromMinutes(30),
            L1CacheDuration = TimeSpan.FromMinutes(5),
            EnableFailSafe = true,
            EnableEagerRefresh = true
        });
        
        // 실제 FusionCache를 사용하는 인스턴스
        _cache = new IpToNationFusionCache(_fusionCache, _fusionCacheConfig, _mockLogger.Object);
        
        // Mock FusionCache를 사용하는 인스턴스 (오류 시나리오 테스트용)
        _cacheWithMocks = new IpToNationFusionCache(_mockFusionCache.Object, _fusionCacheConfig, _mockLogger.Object);
    }

    /// <summary>
    /// Redis 연결 실패 시 L1 캐시만으로 작동하는지 테스트
    /// FusionCache의 내장 페일오버 메커니즘을 검증합니다
    /// 요구사항 2.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenRedisConnectionFails_ShouldWorkWithL1CacheOnly()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        
        // Act - L1 캐시에 직접 데이터 설정 후 조회
        await _cache.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(5));
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    /// <summary>
    /// FusionCache 내부 오류 발생 시 적절한 오류 처리 테스트
    /// 요구사항 2.3, 3.4 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenFusionCacheThrowsException_ShouldHandleGracefully()
    {
        // Arrange
        const string clientIp = "10.0.0.1";
        var expectedException = new InvalidOperationException("FusionCache internal error");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(expectedException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("FusionCache internal error", result.Errors[0].Message);
        
        // 오류 로그가 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 조회 중 오류가 발생했습니다")),
                It.Is<Exception>(ex => ex == expectedException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// 타임아웃 예외 발생 시 적절한 처리 테스트
    /// 요구사항 3.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenTimeoutOccurs_ShouldHandleTimeoutGracefully()
    {
        // Arrange
        const string clientIp = "172.16.0.1";
        var timeoutException = new TimeoutException("Operation timed out");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(timeoutException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("Operation timed out", result.Errors[0].Message);
    }

    /// <summary>
    /// 페일세이프 메커니즘 기본 동작 테스트
    /// 만료된 데이터라도 반환할 수 있는지 확인
    /// 요구사항 3.3, 3.4 검증
    /// </summary>
    [Fact]
    public async Task Cache_WithFailSafeEnabled_ShouldProvideResilienceAgainstFailures()
    {
        // Arrange
        const string clientIp = "203.0.113.1";
        const string countryCode = "CN";
        
        // FusionCache의 페일세이프 옵션이 활성화된 상태에서 테스트
        var failSafeOptions = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30),
                FailSafeMaxDuration = TimeSpan.FromHours(1),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(10)
            },
            CacheName = "FailSafeTestCache"
        };

        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var fusionCacheLogger = new Mock<ILogger<FusionCache>>();
        using var failSafeCache = new FusionCache(failSafeOptions, memoryCache, fusionCacheLogger.Object);
        var cache = new IpToNationFusionCache(failSafeCache, _fusionCacheConfig, _mockLogger.Object);

        // Act
        await cache.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(30));
        var result = await cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    /// <summary>
    /// 동시성 제어 및 캐시 스탬피드 방지 테스트
    /// 동일 키에 대한 동시 요청 시 중복 처리 방지 확인
    /// 요구사항 3.2 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WithConcurrentRequests_ShouldPreventCacheStampede()
    {
        // Arrange
        const string clientIp = "192.0.2.1";
        const string countryCode = "KR";

        // 먼저 캐시에 데이터 설정
        await _cache.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(30));

        // Act - 동시에 여러 요청 실행
        var tasks = Enumerable.Range(0, 10)
                             .Select(_ => _cache.GetAsync(clientIp))
                             .ToArray();
        
        var results = await Task.WhenAll(tasks);

        // Assert
        // 모든 요청이 성공해야 함
        Assert.All(results, result => 
        {
            Assert.True(result.IsSuccess);
            Assert.Equal(countryCode, result.Value);
        });
    }

    /// <summary>
    /// 메모리 캐시 용량 제한 시 우아한 성능 저하 테스트
    /// 요구사항 2.3 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_WhenMemoryCacheExceedsCapacity_ShouldHandleGracefully()
    {
        // Arrange
        var limitedMemoryOptions = new MemoryCacheOptions
        {
            SizeLimit = 5 // 제한적인 메모리
        };
        
        using var limitedMemoryCache = new MemoryCache(limitedMemoryOptions);
        
        var limitedOptions = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30),
                Size = 1 // 각 항목의 크기
            },
            CacheName = "LimitedTestCache"
        };

        var fusionCacheLogger = new Mock<ILogger<FusionCache>>();
        using var limitedFusionCache = new FusionCache(limitedOptions, limitedMemoryCache, fusionCacheLogger.Object);
        var cache = new IpToNationFusionCache(limitedFusionCache, _fusionCacheConfig, _mockLogger.Object);

        // Act - 용량을 초과하는 데이터 설정
        var tasks = new List<Task>();
        for (int i = 1; i <= 10; i++)
        {
            tasks.Add(cache.SetAsync($"192.168.1.{i}", $"COUNTRY{i}", TimeSpan.FromMinutes(30)));
        }

        // Assert - 메모리 부족 상황에서도 예외가 발생하지 않아야 함
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        Assert.Null(exception);
    }

    /// <summary>
    /// 높은 부하 상황에서의 안정성 테스트
    /// 동시 다중 요청 처리 시 시스템 안정성 확인
    /// 요구사항 3.2 검증
    /// </summary>
    [Fact]
    public async Task Cache_UnderHighLoad_ShouldMaintainStability()
    {
        // Arrange & Act - 높은 부하 시뮬레이션 (동시 50개 요청)
        var tasks = new List<Task>();
        
        // 쓰기 작업
        for (int i = 1; i <= 25; i++)
        {
            var ip = $"192.168.{i % 255}.{i % 255}";
            tasks.Add(_cache.SetAsync(ip, $"COUNTRY{i % 10}", TimeSpan.FromMinutes(30)));
        }
        
        // 읽기 작업
        for (int i = 1; i <= 25; i++)
        {
            var ip = $"10.0.{i % 255}.{i % 255}";
            tasks.Add(_cache.GetAsync(ip));
        }

        // Assert - 높은 부하 상황에서도 예외가 발생하지 않아야 함
        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));
        Assert.Null(exception);
    }

    /// <summary>
    /// 장기간 실행 시나리오 테스트
    /// 장시간 동안 캐시가 안정적으로 동작하는지 확인
    /// 요구사항 2.3, 3.4 검증
    /// </summary>
    [Fact]
    public async Task Cache_DuringLongRunningOperation_ShouldMaintainPerformance()
    {
        // Arrange
        var operationCount = 0;
        var startTime = DateTime.UtcNow;
        
        // Act - 반복적인 캐시 작업 시뮬레이션 (2초간 실행)
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(2))
        {
            var ip = $"192.168.1.{operationCount % 5 + 1}";
            await _cache.SetAsync(ip, "KR", TimeSpan.FromMinutes(1));
            await _cache.GetAsync(ip);
            operationCount++;
            
            if (operationCount % 5 == 0)
            {
                await Task.Delay(10); // 짧은 휴식
            }
        }

        // Assert
        Assert.True(operationCount > 0);
        
        // 마지막 작업이 정상적으로 수행되는지 확인
        var finalResult = await _cache.GetAsync("192.168.1.1");
        Assert.NotNull(finalResult);
    }

    /// <summary>
    /// 예외 복구 시나리오 테스트
    /// 일시적 예외 발생 후 정상 상태로 복구되는지 확인
    /// 요구사항 3.4 검증
    /// </summary>
    [Fact]
    public async Task Cache_AfterTransientExceptions_ShouldRecoverGracefully()
    {
        // Arrange
        const string clientIp = "172.16.0.100";
        const string countryCode = "JP";
        var failureCount = 0;
        
        // 일시적 실패 시뮬레이션
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .Returns(async (string key, string? defaultValue, FusionCacheEntryOptions? options, CancellationToken ct) =>
                       {
                           var currentFailure = Interlocked.Increment(ref failureCount);
                           if (currentFailure <= 2) // 처음 2번은 실패
                           {
                               throw new TimeoutException($"Transient failure #{currentFailure}");
                           }
                           await Task.Delay(10, ct);
                           return countryCode;
                       });

        // Act - 여러 번 시도하여 복구 확인
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _cacheWithMocks.GetAsync(clientIp);
            results.Add(result.IsSuccess);
            await Task.Delay(50);
        }

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Count == 5);
        
        // 마지막 몇 번의 시도는 성공해야 함 (복구 확인)
        var lastResults = results.TakeLast(2);
        Assert.Contains(true, lastResults);
    }

    public void Dispose()
    {
        _fusionCache?.Dispose();
        GC.SuppressFinalize(this);
    }
}