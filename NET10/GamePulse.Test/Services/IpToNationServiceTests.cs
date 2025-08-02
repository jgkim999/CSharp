using FluentAssertions;

using FluentResults;

using GamePulse.Repositories;
using GamePulse.Repositories.IpToNation.Cache;
using GamePulse.Services.IpToNation;

using Moq;

namespace GamePulse.Test.Services;

public class IpToNationServiceTests
{
    private readonly Mock<IIpToNationCache> _mockCache;
    private readonly Mock<IIpToNationRepository> _mockRepo;
    private readonly IpToNationService _service;

    public IpToNationServiceTests()
    {
        _mockCache = new Mock<IIpToNationCache>();
        _mockRepo = new Mock<IIpToNationRepository>();
        _service = new IpToNationService(_mockCache.Object, _mockRepo.Object);
    }

    [Fact]
    public async Task GetNationCodeAsync_CacheHit_ReturnsFromCache()
    {
        // Arrange
        var ip = "192.168.1.1";
        var expectedCountry = "US";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Ok(expectedCountry));

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockRepo.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetNationCodeAsync_CacheMiss_ReturnsFromRepository()
    {
        // Arrange
        var ip = "192.168.1.1";
        var expectedCountry = "KR";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_CacheReturnsFailResult_FallsBackToRepository()
    {
        // Arrange
        var ip = "203.0.113.1";
        var expectedCountry = "JP";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Cache miss"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ip), Times.Once);
        _mockRepo.Verify(x => x.GetAsync(ip), Times.Once);
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Theory]
    [InlineData("8.8.8.8", "US")]
    [InlineData("1.1.1.1", "AU")]
    [InlineData("208.67.222.222", "US")]
    [InlineData("77.88.8.8", "RU")]
    [InlineData("194.71.107.80", "DE")]
    public async Task GetNationCodeAsync_WithValidPublicIps_ReturnsExpectedCountryCodes(string ip, string expectedCountry)
    {
        // Arrange
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ip), Times.Once);
        _mockRepo.Verify(x => x.GetAsync(ip), Times.Once);
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Theory]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("127.0.0.1")]
    public async Task GetNationCodeAsync_WithPrivateIps_ProcessesCorrectly(string ip)
    {
        // Arrange
        var expectedCountry = "XX";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ip), Times.Once);
        _mockRepo.Verify(x => x.GetAsync(ip), Times.Once);
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }
    
    [Fact]
    public async Task GetNationCodeAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var ip = "203.0.113.10";
        var expectedException = new InvalidOperationException("Database connection failed");
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ThrowsAsync(expectedException);

        // Act & Assert
        await FluentActions.Invoking(() => _service.GetNationCodeAsync(ip, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");

        _mockCache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task GetNationCodeAsync_CacheSetThrowsException_DoesNotAffectResult()
    {
        // Arrange
        var ip = "0.0.0.0";
        var expectedCountry = "GB";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockRepo.Verify(x => x.GetAsync(ip), Times.Once);
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("KR")]
    [InlineData("JP")]
    [InlineData("GB")]
    [InlineData("DE")]
    [InlineData("FR")]
    [InlineData("CA")]
    [InlineData("AU")]
    [InlineData("BR")]
    [InlineData("IN")]
    public async Task GetNationCodeAsync_WithVariousCountryCodes_ReturnsCorrectValues(string countryCode)
    {
        // Arrange
        var ip = "203.0.113.20";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Ok(countryCode));

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(countryCode);
        _mockCache.Verify(x => x.GetAsync(ip), Times.Once);
        _mockRepo.Verify(x => x.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetNationCodeAsync_MultipleCallsSameIp_UsesCache()
    {
        // Arrange
        var ip = "203.0.113.25";
        var expectedCountry = "IT";
        _mockCache.SetupSequence(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"))
            .ReturnsAsync(Result.Ok(expectedCountry));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var result1 = await _service.GetNationCodeAsync(ip, CancellationToken.None);
        var result2 = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result1.Should().Be(expectedCountry);
        result2.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ip), Times.Exactly(2));
        _mockRepo.Verify(x => x.GetAsync(ip), Times.Once);
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_IPv6Address_ProcessesCorrectly()
    {
        // Arrange
        var ipv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
        var expectedCountry = "NL";
        _mockCache.Setup(x => x.GetAsync(ipv6))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ipv6))
            .ReturnsAsync(expectedCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ipv6, CancellationToken.None);

        // Assert
        result.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ipv6), Times.Once);
        _mockRepo.Verify(x => x.GetAsync(ipv6), Times.Once);
        _mockCache.Verify(x => x.SetAsync(ipv6, expectedCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_ConcurrentCalls_HandledCorrectly()
    {
        // Arrange
        var ip = "203.0.113.30";
        var expectedCountry = "ES";
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _service.GetNationCodeAsync(ip, CancellationToken.None))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBe(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ip), Times.AtLeast(1));
        _mockRepo.Verify(x => x.GetAsync(ip), Times.AtLeast(1));
    }

    [Fact]
    public async Task GetNationCodeAsync_WithCancellationToken_PassedCorrectly()
    {
        // Arrange
        var ip = "203.0.113.35";
        var expectedCountry = "SE";
        var cancellationToken = new CancellationToken();
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Ok(expectedCountry));

        // Act
        var result = await _service.GetNationCodeAsync(ip, cancellationToken);

        // Assert
        result.Should().Be(expectedCountry);
        _mockCache.Verify(x => x.GetAsync(ip), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_EmptyStringFromRepository_ReturnsEmptyString()
    {
        // Arrange
        var ip = "203.0.113.40";
        var emptyCountry = string.Empty;
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(emptyCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().Be(emptyCountry);
        _mockCache.Verify(x => x.SetAsync(ip, emptyCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_NullFromRepository_ReturnsNull()
    {
        // Arrange
        var ip = "203.0.113.45";
        string? nullCountry = null;
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(nullCountry);

        // Act
        var result = await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        _mockCache.Verify(x => x.SetAsync(ip, nullCountry, TimeSpan.FromDays(1)), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_CacheExpiresAfterOneDay_VerifiesTimeSpan()
    {
        // Arrange
        var ip = "203.0.113.50";
        var expectedCountry = "NO";
        var expectedTimeSpan = TimeSpan.FromDays(1);
        _mockCache.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));
        _mockRepo.Setup(x => x.GetAsync(ip))
            .ReturnsAsync(expectedCountry);

        // Act
        await _service.GetNationCodeAsync(ip, CancellationToken.None);

        // Assert
        _mockCache.Verify(x => x.SetAsync(ip, expectedCountry, expectedTimeSpan), Times.Once);
    }
}