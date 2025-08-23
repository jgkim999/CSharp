using Demo.Domain.Repositories;
using FluentAssertions;
using FluentResults;
using Moq;
using Bogus;

namespace Demo.Domain.Tests.Repositories;

public class IIpToNationCacheTests
{
    private readonly Mock<IIpToNationCache> _mockCache;
    private readonly Faker _faker;

    public IIpToNationCacheTests()
    {
        _mockCache = new Mock<IIpToNationCache>();
        _faker = new Faker();
    }

    [Theory]
    [InlineData("192.168.1.1", "US")]
    [InlineData("10.0.0.1", "KR")]
    [InlineData("172.16.0.1", "JP")]
    public async Task GetAsync_Should_Return_Success_Result_When_IP_Exists_In_Cache(string ip, string countryCode)
    {
        // Arrange
        _mockCache
            .Setup(c => c.GetAsync(ip))
            .ReturnsAsync(Result.Ok(countryCode));

        // Act
        var result = await _mockCache.Object.GetAsync(ip);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Value.Should().Be(countryCode);

        _mockCache.Verify(
            c => c.GetAsync(ip),
            Times.Once);
    }

    [Theory]
    [InlineData("192.168.1.100")]
    [InlineData("10.0.0.200")]
    [InlineData("172.16.0.300")]
    public async Task GetAsync_Should_Return_Failure_Result_When_IP_Not_Found_In_Cache(string ip)
    {
        // Arrange
        _mockCache
            .Setup(c => c.GetAsync(ip))
            .ReturnsAsync(Result.Fail("Not found"));

        // Act
        var result = await _mockCache.Object.GetAsync(ip);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Message.Should().Be("Not found");

        _mockCache.Verify(
            c => c.GetAsync(ip),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_Should_Store_IP_And_Country_Code_In_Cache()
    {
        // Arrange
        var ip = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();
        var timeSpan = TimeSpan.FromHours(1);

        _mockCache
            .Setup(c => c.SetAsync(ip, countryCode, timeSpan))
            .Returns(Task.CompletedTask);

        // Act
        await _mockCache.Object.SetAsync(ip, countryCode, timeSpan);

        // Assert
        _mockCache.Verify(
            c => c.SetAsync(ip, countryCode, timeSpan),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(3600)]
    [InlineData(86400)]
    public async Task SetAsync_Should_Accept_Different_Time_Spans(int seconds)
    {
        // Arrange
        var ip = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();
        var timeSpan = TimeSpan.FromSeconds(seconds);

        _mockCache
            .Setup(c => c.SetAsync(ip, countryCode, timeSpan))
            .Returns(Task.CompletedTask);

        // Act
        await _mockCache.Object.SetAsync(ip, countryCode, timeSpan);

        // Assert
        _mockCache.Verify(
            c => c.SetAsync(ip, countryCode, timeSpan),
            Times.Once);
    }

    [Fact]
    public async Task Cache_Should_Support_Write_Then_Read_Pattern()
    {
        // Arrange
        var ip = "192.168.1.50";
        var countryCode = "US";
        var timeSpan = TimeSpan.FromMinutes(30);

        _mockCache
            .Setup(c => c.SetAsync(ip, countryCode, timeSpan))
            .Returns(Task.CompletedTask);

        _mockCache
            .Setup(c => c.GetAsync(ip))
            .ReturnsAsync(Result.Ok(countryCode));

        // Act
        await _mockCache.Object.SetAsync(ip, countryCode, timeSpan);
        var result = await _mockCache.Object.GetAsync(ip);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(countryCode);

        _mockCache.Verify(
            c => c.SetAsync(ip, countryCode, timeSpan),
            Times.Once);
        _mockCache.Verify(
            c => c.GetAsync(ip),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Multiple_Concurrent_Operations()
    {
        // Arrange
        var operations = new List<Task>();
        var ipCountryPairs = GenerateIpCountryPairs(10);

        foreach (var (ip, country) in ipCountryPairs)
        {
            _mockCache
                .Setup(c => c.SetAsync(ip, country, It.IsAny<TimeSpan>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        foreach (var (ip, country) in ipCountryPairs)
        {
            operations.Add(_mockCache.Object.SetAsync(ip, country, TimeSpan.FromHours(1)));
        }

        await Task.WhenAll(operations);

        // Assert
        foreach (var (ip, country) in ipCountryPairs)
        {
            _mockCache.Verify(
                c => c.SetAsync(ip, country, It.IsAny<TimeSpan>()),
                Times.Once);
        }
    }

    [Fact]
    public async Task GetAsync_Should_Handle_Cache_Exception()
    {
        // Arrange
        var ip = _faker.Internet.Ip();

        _mockCache
            .Setup(c => c.GetAsync(ip))
            .ThrowsAsync(new InvalidOperationException("Cache service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockCache.Object.GetAsync(ip));

        _mockCache.Verify(
            c => c.GetAsync(ip),
            Times.Once);
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Cache_Exception()
    {
        // Arrange
        var ip = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();
        var timeSpan = TimeSpan.FromHours(1);

        _mockCache
            .Setup(c => c.SetAsync(ip, countryCode, timeSpan))
            .ThrowsAsync(new InvalidOperationException("Cache service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockCache.Object.SetAsync(ip, countryCode, timeSpan));

        _mockCache.Verify(
            c => c.SetAsync(ip, countryCode, timeSpan),
            Times.Once);
    }

    private List<(string ip, string country)> GenerateIpCountryPairs(int count)
    {
        var pairs = new List<(string, string)>();
        for (int i = 0; i < count; i++)
        {
            pairs.Add((_faker.Internet.Ip(), _faker.Address.CountryCode()));
        }
        return pairs;
    }
}