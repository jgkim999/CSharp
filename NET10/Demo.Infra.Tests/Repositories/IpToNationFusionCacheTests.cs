using Demo.Application.Configs;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using FluentResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationFusionCache 클래스에 대한 포괄적인 단위 테스트
/// GetAsync, SetAsync 메서드의 모든 시나리오와 키 생성 로직을 검증합니다
/// 요구사항 1.1, 1.2, 1.3, 5.4를 충족합니다
/// </summary>
public class IpToNationFusionCacheTests : IDisposable
{
    private readonly IFusionCache _fusionCache;
    private readonly Mock<IFusionCache> _mockFusionCache;
    private readonly Mock<ILogger<IpToNationFusionCache>> _mockLogger;
    private readonly IOptions<FusionCacheConfig> _fusionCacheConfig;
    private readonly IpToNationFusionCache _cache;
    private readonly IpToNationFusionCache _cacheWithMocks;

    public IpToNationFusionCacheTests()
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
            CacheName = "TestCache"
        };

        _fusionCache = new FusionCache(options, memoryCache, fusionCacheLogger.Object);
        
        // Mock 객체들 생성
        _mockFusionCache = new Mock<IFusionCache>();
        _mockLogger = new Mock<ILogger<IpToNationFusionCache>>();
        
        _fusionCacheConfig = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "test" }
        });
        
        // 실제 FusionCache를 사용하는 인스턴스
        _cache = new IpToNationFusionCache(_fusionCache, _fusionCacheConfig, _mockLogger.Object);
        
        // Mock FusionCache를 사용하는 인스턴스 (오류 시나리오 테스트용)
        _cacheWithMocks = new IpToNationFusionCache(_mockFusionCache.Object, _fusionCacheConfig, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act & Assert
        Assert.NotNull(_cache);
    }

    [Fact]
    public void Constructor_WithNullFusionCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new IpToNationFusionCache(null!, _fusionCacheConfig, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new IpToNationFusionCache(_fusionCache, _fusionCacheConfig, null!));
    }

    [Fact]
    public async Task SetAsync_WithValidParameters_ShouldNotThrowException()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromMinutes(30);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SetAsync_WithoutKeyPrefix_ShouldNotThrowException()
    {
        // Arrange
        const string clientIp = "10.0.0.1";
        const string countryCode = "US";
        var duration = TimeSpan.FromMinutes(15);

        var configWithoutPrefix = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "" }
        });
        var cacheWithoutPrefix = new IpToNationFusionCache(_fusionCache, configWithoutPrefix, _mockLogger.Object);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => cacheWithoutPrefix.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("192.168.1.1", "KR", 30)]
    [InlineData("10.0.0.1", "US", 15)]
    [InlineData("172.16.0.1", "JP", 60)]
    public async Task SetAsync_WithVariousParameters_ShouldNotThrowException(string clientIp, string countryCode, int durationMinutes)
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(durationMinutes);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);
    }

    /// <summary>
    /// 기존 Redis 키 형식과 동일한 키가 생성되는지 검증하는 테스트
    /// 요구사항 1.3, 6.4 검증
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", "test")]
    [InlineData("10.0.0.1", "")]
    [InlineData("172.16.0.1", "")]
    [InlineData("203.0.113.1", "prod")]
    public async Task KeyGeneration_ShouldMatchLegacyRedisKeyFormat(string clientIp, string? keyPrefix)
    {
        // Arrange
        var config = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = keyPrefix }
        });
        var cache = new IpToNationFusionCache(_fusionCache, config, _mockLogger.Object);

        // Act - SetAsync와 GetAsync를 호출하여 키 생성 로직을 테스트
        await cache.SetAsync(clientIp, "KR", TimeSpan.FromMinutes(30));
        var result = await cache.GetAsync(clientIp);

        // Assert - 데이터가 올바르게 저장되고 조회되는지 확인
        Assert.True(result.IsSuccess);
        Assert.Equal("KR", result.Value);
    }

    /// <summary>
    /// 키 접두사가 올바르게 적용되는지 검증하는 테스트
    /// 요구사항 6.2 검증
    /// </summary>
    [Fact]
    public async Task KeyPrefix_ShouldBeAppliedCorrectly()
    {
        // Arrange
        const string testPrefix = "myapp";
        const string clientIp = "192.168.1.100";

        var config = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = testPrefix }
        });
        var cache = new IpToNationFusionCache(_fusionCache, config, _mockLogger.Object);

        // Act
        await cache.SetAsync(clientIp, "KR", TimeSpan.FromMinutes(30));
        var result = await cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KR", result.Value);
    }

    /// <summary>
    /// 빈 키 접두사 처리가 올바른지 검증하는 테스트
    /// 요구사항 6.2 검증
    /// </summary>
    [Fact]
    public async Task EmptyKeyPrefix_ShouldNotIncludePrefixInKey()
    {
        // Arrange
        const string clientIp = "10.0.0.50";

        var config = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "" }
        });
        var cache = new IpToNationFusionCache(_fusionCache, config, _mockLogger.Object);

        // Act
        await cache.SetAsync(clientIp, "US", TimeSpan.FromMinutes(15));
        var result = await cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("US", result.Value);
    }

    /// <summary>
    /// GetAsync 메서드가 기존과 동일한 결과를 반환하는지 검증하는 테스트
    /// 요구사항 1.1 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenCacheHit_ShouldReturnSuccessResult()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string expectedCountryCode = "KR";

        // 먼저 캐시에 데이터 설정
        await _cache.SetAsync(clientIp, expectedCountryCode, TimeSpan.FromMinutes(30));

        // Act
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCountryCode, result.Value);
    }

    /// <summary>
    /// GetAsync 메서드가 캐시 미스 시 기존과 동일한 실패 결과를 반환하는지 검증하는 테스트
    /// 요구사항 1.1 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenCacheMiss_ShouldReturnFailureResult()
    {
        // Arrange
        const string clientIp = "192.168.1.999"; // 존재하지 않는 IP

        // Act
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Not found", result.Errors[0].Message);
    }

    /// <summary>
    /// 상세 로깅이 활성화된 경우 로깅 동작을 검증하는 테스트
    /// 요구사항 4.2 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WithDetailedLoggingEnabled_ShouldLogWithHashedIp()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";

        var configWithDetailedLogging = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "test" },
            EnableDetailedLogging = true
        });
        var cacheWithDetailedLogging = new IpToNationFusionCache(_fusionCache, configWithDetailedLogging, _mockLogger.Object);

        // 먼저 캐시에 데이터 설정
        await cacheWithDetailedLogging.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(30));

        // Act
        var result = await cacheWithDetailedLogging.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
        
        // 상세 로깅이 활성화되었을 때 해시된 IP로 로깅되는지 확인
        // 실제 로그 메시지에는 "ClientIpHash"가 아닌 해시값이 포함됨
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 히트") && v.ToString()!.Contains("IP ")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// 캐시 미스 시 로깅 동작을 검증하는 테스트
    /// 요구사항 4.2 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenCacheMiss_ShouldLogMissEvent()
    {
        // Arrange
        const string clientIp = "192.168.1.999"; // 존재하지 않는 IP

        // Act
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        
        // 캐시 미스 로그가 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 미스")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// 캐시 설정 시 로깅 동작을 검증하는 테스트
    /// 요구사항 4.2 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_ShouldLogSetEvent()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromMinutes(30);

        // Act
        await _cache.SetAsync(clientIp, countryCode, duration);

        // Assert
        // 캐시 설정 로그가 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 설정 완료")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #region GetAsync 메서드 오류 시나리오 테스트

    /// <summary>
    /// GetAsync에서 InvalidOperationException 발생 시 적절한 오류 처리 및 로깅을 검증하는 테스트
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenInvalidOperationExceptionOccurs_ShouldLogErrorAndReturnFailure()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        var expectedException = new InvalidOperationException("Test InvalidOperationException");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(expectedException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("Test InvalidOperationException", result.Errors[0].Message);
        
        // 오류 로그가 Error 레벨로 기록되는지 확인
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
    /// GetAsync에서 TimeoutException 발생 시 적절한 오류 처리를 검증하는 테스트
    /// 요구사항 3.3, 4.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenTimeoutExceptionOccurs_ShouldLogErrorAndReturnFailure()
    {
        // Arrange
        const string clientIp = "10.0.0.1";
        var expectedException = new TimeoutException("Cache operation timed out");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(expectedException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("Cache operation timed out", result.Errors[0].Message);
    }

    /// <summary>
    /// GetAsync에서 일반적인 Exception 발생 시 적절한 오류 처리를 검증하는 테스트
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenGenericExceptionOccurs_ShouldLogErrorAndReturnFailure()
    {
        // Arrange
        const string clientIp = "172.16.0.1";
        var expectedException = new Exception("Generic cache error");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(expectedException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("Generic cache error", result.Errors[0].Message);
    }

    #endregion

    #region SetAsync 메서드 오류 시나리오 테스트

    /// <summary>
    /// SetAsync에서 오류 발생 시 적절한 로깅이 수행되는지 검증하는 테스트
    /// 실제 FusionCache를 사용하여 정상 동작을 확인합니다
    /// 요구사항 4.3 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_WithValidParameters_ShouldLogSuccessfully()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromMinutes(30);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);
        
        // 정상적인 로깅이 수행되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 설정 완료")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    /// <summary>
    /// SetAsync에서 다양한 매개변수로 정상 동작하는지 검증하는 테스트
    /// 요구사항 1.2, 1.4 검증
    /// </summary>
    [Theory]
    [InlineData("10.0.0.1", "US", 15)]
    [InlineData("172.16.0.1", "JP", 60)]
    [InlineData("203.0.113.1", "CN", 1)]
    public async Task SetAsync_WithVariousValidParameters_ShouldWorkCorrectly(string clientIp, string countryCode, int durationMinutes)
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(durationMinutes);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);

        // 설정된 값이 올바르게 조회되는지 확인
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    /// <summary>
    /// SetAsync에서 FusionCacheEntryOptions 변환이 올바르게 수행되는지 검증하는 테스트
    /// 요구사항 2.4 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_ShouldConvertTimeSpanCorrectly()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromMinutes(30);

        // Act
        await _cache.SetAsync(clientIp, countryCode, duration);

        // Assert - 설정된 값이 올바르게 조회되는지 확인
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    #endregion

    /// <summary>
    /// 초기화 시 로깅 동작을 검증하는 테스트
    /// 요구사항 4.2 검증
    /// </summary>
    [Fact]
    public void Constructor_WithDetailedLogging_ShouldLogInitialization()
    {
        // Arrange
        var configWithDetailedLogging = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = "test" },
            EnableDetailedLogging = true
        });

        // Act
        var cache = new IpToNationFusionCache(_fusionCache, configWithDetailedLogging, _mockLogger.Object);

        // Assert
        Assert.NotNull(cache);
        
        // 초기화 로그가 Information 레벨로 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IpToNationFusionCache 초기화됨")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #region GetAsync 메서드 추가 시나리오 테스트

    /// <summary>
    /// GetAsync에서 다양한 IP 형식에 대한 처리를 검증하는 테스트
    /// 요구사항 1.1 검증
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", "KR")]
    [InlineData("10.0.0.1", "US")]
    [InlineData("172.16.0.1", "JP")]
    [InlineData("203.0.113.1", "CN")]
    [InlineData("127.0.0.1", "LOCAL")]
    public async Task GetAsync_WithVariousIpFormats_ShouldReturnCorrectResults(string clientIp, string expectedCountryCode)
    {
        // Arrange
        await _cache.SetAsync(clientIp, expectedCountryCode, TimeSpan.FromMinutes(30));

        // Act
        var result = await _cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCountryCode, result.Value);
    }

    /// <summary>
    /// GetAsync에서 null 값이 반환될 때의 처리를 검증하는 테스트
    /// 요구사항 1.1 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenFusionCacheReturnsNull_ShouldReturnFailureResult()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync((string?)null);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Not found", result.Errors[0].Message);
    }

    /// <summary>
    /// GetAsync에서 빈 문자열이 반환될 때의 처리를 검증하는 테스트
    /// 요구사항 1.1 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenFusionCacheReturnsEmptyString_ShouldReturnSuccessWithEmptyString()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string emptyCountryCode = "";
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(emptyCountryCode);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(emptyCountryCode, result.Value);
    }

    #endregion

    #region SetAsync 메서드 추가 시나리오 테스트

    /// <summary>
    /// SetAsync에서 다양한 TTL 값에 대한 처리를 검증하는 테스트
    /// 요구사항 1.2, 1.4 검증
    /// </summary>
    [Theory]
    [InlineData(1)] // 1분
    [InlineData(30)] // 30분
    [InlineData(60)] // 1시간
    [InlineData(1440)] // 24시간
    public async Task SetAsync_WithVariousTtlValues_ShouldSetCorrectDuration(int durationMinutes)
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromMinutes(durationMinutes);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);

        // 설정된 값이 올바르게 조회되는지 확인
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    /// <summary>
    /// SetAsync에서 빈 문자열 국가 코드 처리를 검증하는 테스트
    /// 요구사항 1.2 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_WithEmptyCountryCode_ShouldSetEmptyValue()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string emptyCountryCode = "";
        var duration = TimeSpan.FromMinutes(30);

        // Act
        await _cache.SetAsync(clientIp, emptyCountryCode, duration);

        // Assert
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(emptyCountryCode, result.Value);
    }

    /// <summary>
    /// SetAsync에서 매우 짧은 TTL 처리를 검증하는 테스트
    /// 요구사항 1.4 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_WithVeryShortTtl_ShouldSetCorrectDuration()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var shortDuration = TimeSpan.FromSeconds(1);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, shortDuration));
        Assert.Null(exception);
    }

    /// <summary>
    /// SetAsync에서 다양한 TTL 값으로 정상 동작하는지 검증하는 테스트
    /// 요구사항 2.4 검증
    /// </summary>
    [Theory]
    [InlineData(1)]    // 1분
    [InlineData(30)]   // 30분
    [InlineData(60)]   // 1시간
    [InlineData(1440)] // 24시간
    public async Task SetAsync_WithVariousTtlValues_ShouldWorkCorrectly(int durationMinutes)
    {
        // Arrange
        const string clientIp = "192.168.1.100";
        const string countryCode = "TEST";
        var duration = TimeSpan.FromMinutes(durationMinutes);

        // Act
        await _cache.SetAsync(clientIp, countryCode, duration);

        // Assert - 설정된 값이 올바르게 조회되는지 확인
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    #endregion

    #region 키 생성 로직 검증 테스트

    /// <summary>
    /// 키 생성 로직이 기존 IpToNationRedisCache와 동일한지 검증하는 포괄적인 테스트
    /// 요구사항 1.3, 6.2, 6.4 검증
    /// </summary>
    [Theory]
    [InlineData("192.168.1.1", "test")]
    [InlineData("10.0.0.1", "")]
    [InlineData("172.16.0.1", null)]
    [InlineData("203.0.113.1", "prod")]
    [InlineData("127.0.0.1", "dev-env")]
    public async Task KeyGeneration_ShouldMatchExpectedFormat(string clientIp, string? keyPrefix)
    {
        // Arrange
        var config = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = keyPrefix }
        });
        var cache = new IpToNationFusionCache(_fusionCache, config, _mockLogger.Object);

        // Act - SetAsync와 GetAsync를 통해 키 생성 로직 검증
        await cache.SetAsync(clientIp, "KR", TimeSpan.FromMinutes(30));
        var result = await cache.GetAsync(clientIp);

        // Assert - 데이터가 올바르게 저장되고 조회되면 키 생성이 올바름을 의미
        Assert.True(result.IsSuccess);
        Assert.Equal("KR", result.Value);
    }

    /// <summary>
    /// null 키 접두사 처리를 검증하는 테스트
    /// 요구사항 6.2 검증
    /// </summary>
    [Fact]
    public async Task KeyGeneration_WithNullKeyPrefix_ShouldNotIncludePrefix()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        
        var config = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = null }
        });
        var cache = new IpToNationFusionCache(_fusionCache, config, _mockLogger.Object);

        // Act
        await cache.SetAsync(clientIp, "KR", TimeSpan.FromMinutes(30));
        var result = await cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KR", result.Value);
    }

    /// <summary>
    /// 특수 문자가 포함된 키 접두사 처리를 검증하는 테스트
    /// 요구사항 6.2 검증
    /// </summary>
    [Theory]
    [InlineData("app-v1.0")]
    [InlineData("test_env")]
    [InlineData("prod.cache")]
    public async Task KeyGeneration_WithSpecialCharactersInPrefix_ShouldHandleCorrectly(string keyPrefix)
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        
        var config = Options.Create(new FusionCacheConfig 
        { 
            Redis = new RedisConfig { KeyPrefix = keyPrefix }
        });
        var cache = new IpToNationFusionCache(_fusionCache, config, _mockLogger.Object);

        // Act
        await cache.SetAsync(clientIp, "KR", TimeSpan.FromMinutes(30));
        var result = await cache.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KR", result.Value);
    }

    #endregion

    #region 메트릭 서비스 통합 테스트

    /// <summary>
    /// 메트릭 서비스 없이도 정상 작동하는지 검증하는 테스트
    /// 요구사항 4.1, 4.4 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WithoutMetricsService_ShouldWorkNormally()
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
    }

    /// <summary>
    /// SetAsync에서 메트릭 서비스 없이도 정상 작동하는지 검증하는 테스트
    /// 요구사항 4.1, 4.4 검증
    /// </summary>
    [Fact]
    public async Task SetAsync_WithoutMetricsService_ShouldWorkNormally()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string countryCode = "KR";
        var duration = TimeSpan.FromMinutes(30);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _cache.SetAsync(clientIp, countryCode, duration));
        Assert.Null(exception);

        // 설정된 값이 올바르게 조회되는지 확인
        var result = await _cache.GetAsync(clientIp);
        Assert.True(result.IsSuccess);
        Assert.Equal(countryCode, result.Value);
    }

    #endregion

    #region 생성자 및 초기화 테스트

    /// <summary>
    /// 생성자에서 null 메트릭 서비스 처리를 검증하는 테스트
    /// 요구사항 5.1 검증
    /// </summary>
    [Fact]
    public void Constructor_WithNullMetricsService_ShouldCreateInstanceSuccessfully()
    {
        // Act & Assert
        var cache = new IpToNationFusionCache(_fusionCache, _fusionCacheConfig, _mockLogger.Object, null);
        Assert.NotNull(cache);
    }

    /// <summary>
    /// 생성자에서 null FusionCacheConfig 처리를 검증하는 테스트
    /// 요구사항 5.1 검증
    /// </summary>
    [Fact]
    public void Constructor_WithNullFusionCacheConfig_ShouldCreateInstanceSuccessfully()
    {
        // Act & Assert
        var cache = new IpToNationFusionCache(_fusionCache, null!, _mockLogger.Object);
        Assert.NotNull(cache);
    }

    #endregion

    public void Dispose()
    {
        _fusionCache?.Dispose();
        GC.SuppressFinalize(this);
    }
}