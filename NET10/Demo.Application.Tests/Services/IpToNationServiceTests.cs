using Demo.Application.Services;
using Demo.Domain.Repositories;
using FluentAssertions;
using FluentResults;
using Moq;
using System.Diagnostics;

namespace Demo.Application.Tests.Services;

/// <summary>
/// IpToNationService 클래스의 단위 테스트
/// IP 주소를 통한 국가 코드 조회 서비스 테스트
/// </summary>
public class IpToNationServiceTests
{
    private readonly Mock<IIpToNationCache> _mockCache;
    private readonly Mock<IIpToNationRepository> _mockRepository;
    private readonly Mock<ITelemetryService> _mockTelemetryService;
    private readonly Mock<Activity> _mockActivity;
    private readonly IpToNationService _service;

    public IpToNationServiceTests()
    {
        _mockCache = new Mock<IIpToNationCache>();
        _mockRepository = new Mock<IIpToNationRepository>();
        _mockTelemetryService = new Mock<ITelemetryService>();
        _mockActivity = new Mock<Activity>("test");

        // StartActivity가 호출될 때 mock activity 반환
        _mockTelemetryService
            .Setup(x => x.StartActivity(It.IsAny<string>(), It.IsAny<Dictionary<string, object?>>()))
            .Returns(_mockActivity.Object);

        _service = new IpToNationService(_mockCache.Object, _mockRepository.Object, _mockTelemetryService.Object);
    }

    [Fact]
    public async Task GetNationCodeAsync_ShouldCallRepositoryAndReturnCountryCode()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string expectedCountryCode = "KR";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedCountryCode);

        // Act
        var result = await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        result.Should().Be(expectedCountryCode);
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
        _mockTelemetryService.Verify(x => x.StartActivity("GetNationCodeAsync", null), Times.Once);
    }

    [Theory]
    [InlineData("1.32.216.1", "KR")]
    [InlineData("8.8.8.8", "US")]
    [InlineData("1.33.100.1", "JP")]
    [InlineData("203.104.144.1", "CN")]
    public async Task GetNationCodeAsync_WithVariousIps_ShouldReturnCorrectCountryCode(string clientIp, string expectedCountryCode)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedCountryCode);

        // Act
        var result = await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        result.Should().Be(expectedCountryCode);
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database connection failed");

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetNationCodeAsync(clientIp, cancellationToken));

        exception.Should().Be(expectedException);
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string expectedCountryCode = "KR";
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedCountryCode);

        // Act
        var result = await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        result.Should().Be(expectedCountryCode);
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_ShouldStartTelemetryActivity()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string expectedCountryCode = "KR";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedCountryCode);

        // Act
        await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        _mockTelemetryService.Verify(x => x.StartActivity("GetNationCodeAsync", null), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-ip")]
    [InlineData("999.999.999.999")]
    public async Task GetNationCodeAsync_WithInvalidIp_ShouldStillCallRepository(string invalidIp)
    {
        // Arrange
        const string expectedCountryCode = "XX";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(invalidIp))
            .ReturnsAsync(expectedCountryCode);

        // Act
        var result = await _service.GetNationCodeAsync(invalidIp, cancellationToken);

        // Assert
        result.Should().Be(expectedCountryCode);
        _mockRepository.Verify(x => x.GetAsync(invalidIp), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_WhenRepositoryReturnsNull_ShouldReturnNull()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(null as string);

        // Act
        var result = await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_WhenRepositoryReturnsEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        result.Should().Be(string.Empty);
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
    }

    [Fact]
    public async Task GetNationCodeAsync_MultipleCallsWithSameIp_ShouldCallRepositoryEachTime()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string expectedCountryCode = "KR";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .ReturnsAsync(expectedCountryCode);

        // Act
        await _service.GetNationCodeAsync(clientIp, cancellationToken);
        await _service.GetNationCodeAsync(clientIp, cancellationToken);
        await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Exactly(3));
        _mockTelemetryService.Verify(x => x.StartActivity("GetNationCodeAsync", null), Times.Exactly(3));
    }

    [Fact]
    public async Task GetNationCodeAsync_WithLongRunningRepository_ShouldNotTimeout()
    {
        // Arrange
        const string clientIp = "192.168.1.1";
        const string expectedCountryCode = "KR";
        var cancellationToken = CancellationToken.None;

        _mockRepository
            .Setup(x => x.GetAsync(clientIp))
            .Returns(async () =>
            {
                await Task.Delay(100, cancellationToken); // 100ms 지연
                return expectedCountryCode;
            });

        // Act
        var result = await _service.GetNationCodeAsync(clientIp, cancellationToken);

        // Assert
        result.Should().Be(expectedCountryCode);
        _mockRepository.Verify(x => x.GetAsync(clientIp), Times.Once);
    }
}