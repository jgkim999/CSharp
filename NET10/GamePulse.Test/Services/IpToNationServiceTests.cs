using FluentAssertions;
using FluentResults;
using GamePulse.Repositories;
using GamePulse.Services;
using Microsoft.Extensions.Logging;
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
}