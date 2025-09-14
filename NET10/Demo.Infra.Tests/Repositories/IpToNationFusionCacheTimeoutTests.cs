using Demo.Application.Configs;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationFusionCache의 타임아웃 시나리오를 테스트하는 클래스
/// 다양한 타임아웃 상황에서의 동작을 검증합니다
/// 요구사항 3.3을 충족합니다
/// </summary>
public class IpToNationFusionCacheTimeoutTests : IDisposable
{
    private readonly Mock<IFusionCache> _mockFusionCache;
    private readonly Mock<ILogger<IpToNationFusionCache>> _mockLogger;
    private readonly IOptions<FusionCacheConfig> _fusionCacheConfig;
    private readonly IpToNationFusionCache _cacheWithMocks;

    public IpToNationFusionCacheTimeoutTests()
    {
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
        
        _cacheWithMocks = new IpToNationFusionCache(_mockFusionCache.Object, _fusionCacheConfig, _mockLogger.Object);
    }

    /// <summary>
    /// 타임아웃 예외 발생 시 적절한 처리 테스트
    /// 요구사항 3.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenTimeoutExceptionOccurs_ShouldHandleGracefully()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        var timeoutException = new TimeoutException("Cache operation timed out");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(timeoutException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("Cache operation timed out", result.Errors[0].Message);
        
        // 타임아웃 오류 로그가 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 조회 중 오류가 발생했습니다")),
                It.Is<Exception>(ex => ex == timeoutException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    /// OperationCanceledException 발생 시 적절한 처리 테스트
    /// 요구사항 3.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WhenOperationCanceledException_ShouldHandleGracefully()
    {
        // Arrange
        const string clientIp = "10.0.0.1";
        var cancellationException = new OperationCanceledException("Operation was cancelled due to timeout");
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(cancellationException);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains("Operation was cancelled due to timeout", result.Errors[0].Message);
    }

    /// <summary>
    /// 다양한 타임아웃 관련 예외 처리 테스트
    /// 요구사항 3.3 검증
    /// </summary>
    [Theory]
    [InlineData(typeof(TimeoutException), "Timeout occurred")]
    [InlineData(typeof(OperationCanceledException), "Operation cancelled")]
    [InlineData(typeof(TaskCanceledException), "Task was cancelled")]
    public async Task GetAsync_WithVariousTimeoutExceptions_ShouldHandleAppropriately(Type exceptionType, string message)
    {
        // Arrange
        const string clientIp = "203.0.113.1";
        var exception = (Exception)Activator.CreateInstance(exceptionType, message)!;
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(exception);

        // Act
        var result = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("캐시 조회 실패", result.Errors[0].Message);
        Assert.Contains(message, result.Errors[0].Message);
    }

    /// <summary>
    /// 타임아웃 후 재시도 시나리오 테스트
    /// 타임아웃 발생 후 재시도 시 정상 동작 확인
    /// 요구사항 3.3, 3.4 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_AfterTimeoutRecovery_ShouldWorkNormally()
    {
        // Arrange
        const string clientIp = "198.51.100.1";
        const string countryCode = "TEST";
        var callCount = 0;
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .Returns(async (string key, string? defaultValue, FusionCacheEntryOptions? options, CancellationToken ct) =>
                       {
                           var currentCall = Interlocked.Increment(ref callCount);
                           if (currentCall == 1) // 첫 번째 호출은 타임아웃
                           {
                               throw new TimeoutException("First call timeout");
                           }
                           await Task.Delay(10, ct);
                           return countryCode;
                       });

        // Act
        // 첫 번째 호출 - 타임아웃 발생
        var firstResult = await _cacheWithMocks.GetAsync(clientIp);
        
        // 두 번째 호출 - 정상 동작
        var secondResult = await _cacheWithMocks.GetAsync(clientIp);

        // Assert
        // 첫 번째 호출은 실패
        Assert.True(firstResult.IsFailed);
        Assert.Contains("First call timeout", firstResult.Errors[0].Message);
        
        // 두 번째 호출은 성공
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(countryCode, secondResult.Value);
    }

    /// <summary>
    /// 타임아웃 발생 빈도 테스트
    /// 연속적인 타임아웃 발생 시 적절한 처리 확인
    /// 요구사항 3.3 검증
    /// </summary>
    [Fact]
    public async Task GetAsync_WithConsecutiveTimeouts_ShouldHandleConsistently()
    {
        // Arrange
        const string clientIp = "192.0.2.100";
        
        _mockFusionCache.Setup(x => x.GetOrDefaultAsync<string>(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<FusionCacheEntryOptions?>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new TimeoutException("Consistent timeout"));

        // Act - 여러 번 연속 호출
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            var result = await _cacheWithMocks.GetAsync($"{clientIp}_{i}");
            results.Add(result.IsFailed);
        }

        // Assert
        // 모든 호출이 일관되게 실패해야 함
        Assert.All(results, failed => Assert.True(failed));
        
        // 타임아웃 로그가 각 호출마다 기록되는지 확인
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("캐시 조회 중 오류가 발생했습니다")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(5));
    }

    public void Dispose()
    {
        // Mock 객체들은 자동으로 정리됨
        GC.SuppressFinalize(this);
    }
}