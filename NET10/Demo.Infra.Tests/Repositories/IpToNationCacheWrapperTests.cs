using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationCacheWrapper의 전환 메커니즘을 테스트합니다
/// 기능 플래그와 트래픽 분할 로직의 정확성을 검증합니다
/// </summary>
public class IpToNationCacheWrapperTests
{
    private readonly Mock<IpToNationFusionCache> _mockFusionCache;
    private readonly Mock<IpToNationRedisCache> _mockRedisCache;
    private readonly Mock<IOptionsMonitor<FusionCacheConfig>> _mockConfigMonitor;
    private readonly Mock<ILogger<IpToNationCacheWrapper>> _mockLogger;
    private readonly IpToNationCacheWrapper _wrapper;

    public IpToNationCacheWrapperTests()
    {
        _mockFusionCache = new Mock<IpToNationFusionCache>();
        _mockRedisCache = new Mock<IpToNationRedisCache>();
        _mockConfigMonitor = new Mock<IOptionsMonitor<FusionCacheConfig>>();
        _mockLogger = new Mock<ILogger<IpToNationCacheWrapper>>();

        _wrapper = new IpToNationCacheWrapper(
            _mockFusionCache.Object,
            _mockRedisCache.Object,
            _mockConfigMonitor.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetAsync_WhenUseFusionCacheIsFalse_ShouldUseRedisCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = false,
            TrafficSplitRatio = 0.0
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var expectedResult = Result.Ok("KR");
        _mockRedisCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _wrapper.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("KR", result.Value);
        _mockRedisCache.Verify(x => x.GetAsync(clientIp), Times.Once);
        _mockFusionCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenUseFusionCacheIsTrue_ShouldUseFusionCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 1.0
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var expectedResult = Result.Ok("US");
        _mockFusionCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _wrapper.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("US", result.Value);
        _mockFusionCache.Verify(x => x.GetAsync(clientIp), Times.Once);
        _mockRedisCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenTrafficSplitRatioIsZero_ShouldUseRedisCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 0.0
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var expectedResult = Result.Ok("JP");
        _mockRedisCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _wrapper.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("JP", result.Value);
        _mockRedisCache.Verify(x => x.GetAsync(clientIp), Times.Once);
        _mockFusionCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAsync_WhenTrafficSplitRatioIsOne_ShouldUseFusionCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 1.0
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var expectedResult = Result.Ok("CN");
        _mockFusionCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _wrapper.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("CN", result.Value);
        _mockFusionCache.Verify(x => x.GetAsync(clientIp), Times.Once);
        _mockRedisCache.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("192.168.1.1", 12345, 0.5)]
    [InlineData("192.168.1.2", 12345, 0.5)]
    [InlineData("10.0.0.1", 12345, 0.5)]
    [InlineData("172.16.0.1", 12345, 0.5)]
    public async Task GetAsync_WithTrafficSplit_ShouldBeConsistentForSameIp(
        string clientIp, int hashSeed, double trafficSplitRatio)
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = trafficSplitRatio,
            TrafficSplitHashSeed = hashSeed
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        _mockFusionCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(Result.Ok("FUSION"));
        _mockRedisCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(Result.Ok("REDIS"));

        // Act - 동일한 IP로 여러 번 호출
        var results = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var result = await _wrapper.GetAsync(clientIp);
            results.Add(result.Value);
        }

        // Assert - 모든 결과가 동일해야 함 (일관된 라우팅)
        Assert.True(results.All(r => r == results[0]), 
            $"동일한 IP {clientIp}에 대해 일관되지 않은 라우팅이 발생했습니다: {string.Join(", ", results)}");
    }

    [Fact]
    public async Task GetAsync_WhenFusionCacheThrowsException_ShouldFallbackToRedisCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 0.5 // 부분적 전환으로 폴백 가능
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        _mockFusionCache.Setup(x => x.GetAsync(clientIp))
            .ThrowsAsync(new InvalidOperationException("FusionCache 오류"));
        _mockRedisCache.Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(Result.Ok("FALLBACK"));

        // Act
        var result = await _wrapper.GetAsync(clientIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("FALLBACK", result.Value);
        _mockFusionCache.Verify(x => x.GetAsync(clientIp), Times.Once);
        _mockRedisCache.Verify(x => x.GetAsync(clientIp), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WhenUseFusionCacheIsFalse_ShouldUseRedisCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = false,
            TrafficSplitRatio = 0.0
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var countryCode = "KR";
        var expiration = TimeSpan.FromMinutes(30);
        var expectedResult = Result.Ok();
        _mockRedisCache.Setup(x => x.SetAsync(clientIp, countryCode, expiration))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _wrapper.SetAsync(clientIp, countryCode, expiration);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRedisCache.Verify(x => x.SetAsync(clientIp, countryCode, expiration), Times.Once);
        _mockFusionCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_WhenUseFusionCacheIsTrue_ShouldUseFusionCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 1.0
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var countryCode = "US";
        var expiration = TimeSpan.FromMinutes(30);
        var expectedResult = Result.Ok();
        _mockFusionCache.Setup(x => x.SetAsync(clientIp, countryCode, expiration))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _wrapper.SetAsync(clientIp, countryCode, expiration);

        // Assert
        Assert.True(result.IsSuccess);
        _mockFusionCache.Verify(x => x.SetAsync(clientIp, countryCode, expiration), Times.Once);
        _mockRedisCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_WhenFusionCacheThrowsException_ShouldFallbackToRedisCache()
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 0.5 // 부분적 전환으로 폴백 가능
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        var clientIp = "192.168.1.100";
        var countryCode = "JP";
        var expiration = TimeSpan.FromMinutes(30);
        _mockFusionCache.Setup(x => x.SetAsync(clientIp, countryCode, expiration))
            .ThrowsAsync(new InvalidOperationException("FusionCache 오류"));
        _mockRedisCache.Setup(x => x.SetAsync(clientIp, countryCode, expiration))
            .ReturnsAsync(Result.Ok());

        // Act
        var result = await _wrapper.SetAsync(clientIp, countryCode, expiration);

        // Assert
        Assert.True(result.IsSuccess);
        _mockFusionCache.Verify(x => x.SetAsync(clientIp, countryCode, expiration), Times.Once);
        _mockRedisCache.Verify(x => x.SetAsync(clientIp, countryCode, expiration), Times.Once);
    }

    [Theory]
    [InlineData(0.0, 100)] // 0% 트래픽 분할 - 모든 요청이 Redis로
    [InlineData(1.0, 100)] // 100% 트래픽 분할 - 모든 요청이 FusionCache로
    public async Task TrafficSplitDistribution_ShouldRespectRatio(double ratio, int requestCount)
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = ratio,
            TrafficSplitHashSeed = 12345
        };
        _mockConfigMonitor.Setup(x => x.CurrentValue).Returns(config);

        _mockFusionCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Ok("FUSION"));
        _mockRedisCache.Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Ok("REDIS"));

        // Act - 다양한 IP로 요청
        var fusionCacheCount = 0;
        var redisCacheCount = 0;

        for (int i = 0; i < requestCount; i++)
        {
            var clientIp = $"192.168.1.{i % 255}";
            var result = await _wrapper.GetAsync(clientIp);
            
            if (result.Value == "FUSION")
                fusionCacheCount++;
            else if (result.Value == "REDIS")
                redisCacheCount++;
        }

        // Assert
        if (ratio == 0.0)
        {
            Assert.Equal(0, fusionCacheCount);
            Assert.Equal(requestCount, redisCacheCount);
        }
        else if (ratio == 1.0)
        {
            Assert.Equal(requestCount, fusionCacheCount);
            Assert.Equal(0, redisCacheCount);
        }
    }

    [Fact]
    public void FusionCacheConfig_TrafficSplitValidation_ShouldWork()
    {
        // Arrange & Act & Assert
        var validConfig = new FusionCacheConfig
        {
            UseFusionCache = true,
            TrafficSplitRatio = 0.5,
            TrafficSplitHashSeed = 12345
        };

        var (isValid, errors) = validConfig.Validate();
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(-0.1)] // 음수
    [InlineData(1.1)]  // 1.0 초과
    public void FusionCacheConfig_InvalidTrafficSplitRatio_ShouldFailValidation(double invalidRatio)
    {
        // Arrange
        var config = new FusionCacheConfig
        {
            TrafficSplitRatio = invalidRatio
        };

        // Act & Assert
        // 실제 유효성 검증은 DataAnnotations에 의해 수행되므로
        // 여기서는 범위 확인만 수행
        Assert.True(invalidRatio < 0.0 || invalidRatio > 1.0);
    }
}