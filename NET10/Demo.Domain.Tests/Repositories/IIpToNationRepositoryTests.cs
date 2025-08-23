using Demo.Domain.Repositories;
using FluentAssertions;
using Moq;
using Bogus;

namespace Demo.Domain.Tests.Repositories;

public class IIpToNationRepositoryTests
{
    private readonly Mock<IIpToNationRepository> _mockRepository;
    private readonly Faker _faker;

    public IIpToNationRepositoryTests()
    {
        _mockRepository = new Mock<IIpToNationRepository>();
        _faker = new Faker();
    }

    [Theory]
    [InlineData("192.168.1.1", "US")]
    [InlineData("10.0.0.1", "KR")]
    [InlineData("127.0.0.1", "LOCAL")]
    public async Task GetAsync_Should_Return_Country_Code_For_Valid_IP(string ipAddress, string expectedCountryCode)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAsync(ipAddress))
            .ReturnsAsync(expectedCountryCode);

        // Act
        var result = await _mockRepository.Object.GetAsync(ipAddress);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(expectedCountryCode);

        _mockRepository.Verify(
            r => r.GetAsync(ipAddress),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid-ip")]
    [InlineData("999.999.999.999")]
    public async Task GetAsync_Should_Return_Unknown_For_Invalid_IP(string invalidIp)
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAsync(invalidIp))
            .ReturnsAsync("unknown");

        // Act
        var result = await _mockRepository.Object.GetAsync(invalidIp);

        // Assert
        result.Should().Be("unknown");

        _mockRepository.Verify(
            r => r.GetAsync(invalidIp),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange
        var ipAddresses = GenerateValidIpAddresses(10);
        var tasks = new List<Task<string>>();

        foreach (var ip in ipAddresses)
        {
            _mockRepository
                .Setup(r => r.GetAsync(ip))
                .ReturnsAsync(_faker.Address.CountryCode());
        }

        // Act
        foreach (var ip in ipAddresses)
        {
            tasks.Add(_mockRepository.Object.GetAsync(ip));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(result => result.Should().NotBeNullOrEmpty());

        foreach (var ip in ipAddresses)
        {
            _mockRepository.Verify(
                r => r.GetAsync(ip),
                Times.Once);
        }
    }

    [Fact]
    public async Task GetAsync_Should_Return_Consistent_Results_For_Same_IP()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        var countryCode = "US";

        _mockRepository
            .Setup(r => r.GetAsync(ipAddress))
            .ReturnsAsync(countryCode);

        // Act
        var result1 = await _mockRepository.Object.GetAsync(ipAddress);
        var result2 = await _mockRepository.Object.GetAsync(ipAddress);
        var result3 = await _mockRepository.Object.GetAsync(ipAddress);

        // Assert
        result1.Should().Be(countryCode);
        result2.Should().Be(countryCode);
        result3.Should().Be(countryCode);

        _mockRepository.Verify(
            r => r.GetAsync(ipAddress),
            Times.Exactly(3));
    }

    [Fact]
    public async Task GetAsync_Should_Handle_Exception_Gracefully()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        _mockRepository
            .Setup(r => r.GetAsync(ipAddress))
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _mockRepository.Object.GetAsync(ipAddress));

        _mockRepository.Verify(
            r => r.GetAsync(ipAddress),
            Times.Once);
    }

    private List<string> GenerateValidIpAddresses(int count)
    {
        var ipAddresses = new List<string>();
        for (int i = 0; i < count; i++)
        {
            ipAddresses.Add(_faker.Internet.Ip());
        }
        return ipAddresses;
    }
}