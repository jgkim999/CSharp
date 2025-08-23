using Demo.Application.Configs;
using Demo.Infra.Tests.TestHelpers;
using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Bogus;
using Testcontainers.Redis;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// Redis TestContainer를 사용한 IpToNationRedisCache 테스트
/// 실제 Redis 인스턴스와의 상호작용을 테스트
/// </summary>
public class IpToNationRedisCacheTests : IAsyncLifetime
{
    private readonly Mock<ILogger<TestableIpToNationRedisCache>> _mockLogger;
    private readonly Faker _faker;
    private RedisContainer? _redisContainer;
    private TestableIpToNationRedisCache? _cache;

    public IpToNationRedisCacheTests()
    {
        _mockLogger = new Mock<ILogger<TestableIpToNationRedisCache>>();
        _faker = new Faker();
    }

    public async Task InitializeAsync()
    {
        // Redis TestContainer 시작
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();

        await _redisContainer.StartAsync();

        // Redis 캐시 인스턴스 생성
        var redisConfig = new RedisConfig
        {
            IpToNationConnectionString = _redisContainer.GetConnectionString(),
            KeyPrefix = "test"
        };

        var mockOptions = new Mock<IOptions<RedisConfig>>();
        mockOptions.Setup(x => x.Value).Returns(redisConfig);

        _cache = new TestableIpToNationRedisCache(mockOptions.Object, _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetAsync_Should_Return_Failure_When_Key_Not_Found()
    {
        // Arrange
        var clientIp = _faker.Internet.Ip();

        // Act
        var result = await _cache!.GetAsync(clientIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        result.Errors.First().Message.Should().Be("Not found");
    }

    [Fact]
    public async Task SetAsync_And_GetAsync_Should_Work_Together()
    {
        // Arrange
        var clientIp = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();
        var expiry = TimeSpan.FromMinutes(30);

        // Act
        await _cache!.SetAsync(clientIp, countryCode, expiry);
        var result = await _cache.GetAsync(clientIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.IsFailed.Should().BeFalse();
        result.Value.Should().Be(countryCode);
    }

    [Fact]
    public async Task SetAsync_Should_Override_Existing_Value()
    {
        // Arrange
        var clientIp = _faker.Internet.Ip();
        var firstCountryCode = "US";
        var secondCountryCode = "KR";
        var expiry = TimeSpan.FromMinutes(30);

        // Act
        await _cache!.SetAsync(clientIp, firstCountryCode, expiry);
        await _cache.SetAsync(clientIp, secondCountryCode, expiry);
        var result = await _cache.GetAsync(clientIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(secondCountryCode);
        result.Value.Should().NotBe(firstCountryCode);
    }

    [Theory]
    [InlineData(60)]
    [InlineData(3600)]
    public async Task SetAsync_Should_Respect_Expiry_Time(int seconds)
    {
        // Arrange
        var clientIp = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();
        var expiry = TimeSpan.FromSeconds(seconds);

        // Act
        await _cache!.SetAsync(clientIp, countryCode, expiry);
        var result = await _cache.GetAsync(clientIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(countryCode);
    }

    [Fact]
    public async Task SetAsync_Should_Handle_Short_Expiry_Time()
    {
        // Arrange
        var clientIp = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();
        var expiry = TimeSpan.FromSeconds(1);

        // Act
        await _cache!.SetAsync(clientIp, countryCode, expiry);
        var result = await _cache.GetAsync(clientIp);

        // Assert - 값이 설정되었는지 확인
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(countryCode);

        // Act - 만료 시간 대기 후 다시 조회
        await Task.Delay(TimeSpan.FromSeconds(2));
        var expiredResult = await _cache.GetAsync(clientIp);

        // Assert - 만료되었는지 확인
        expiredResult.IsSuccess.Should().BeFalse();
        expiredResult.Errors.First().Message.Should().Be("Not found");
    }

    [Theory]
    [InlineData("testapp")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Cache_Should_Work_With_Different_Key_Prefixes(string? keyPrefix)
    {
        // Arrange
        var redisConfig = new RedisConfig
        {
            IpToNationConnectionString = _redisContainer!.GetConnectionString(),
            KeyPrefix = keyPrefix
        };

        var mockOptions = new Mock<IOptions<RedisConfig>>();
        mockOptions.Setup(x => x.Value).Returns(redisConfig);

        var cache = new TestableIpToNationRedisCache(mockOptions.Object, _mockLogger.Object);
        
        var clientIp = _faker.Internet.Ip();
        var countryCode = _faker.Address.CountryCode();

        // Act
        await cache.SetAsync(clientIp, countryCode, TimeSpan.FromMinutes(10));
        var result = await cache.GetAsync(clientIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(countryCode);
    }

}

/// <summary>
/// Redis TestContainer를 사용한 고성능 및 동시성 통합 테스트
/// </summary>
public class IpToNationRedisCacheIntegrationTests : IAsyncLifetime
{
    private readonly Mock<ILogger<TestableIpToNationRedisCache>> _mockLogger;
    private readonly Faker _faker;
    private RedisContainer? _redisContainer;
    private TestableIpToNationRedisCache? _cache;

    public IpToNationRedisCacheIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<TestableIpToNationRedisCache>>();
        _faker = new Faker();
    }

    public async Task InitializeAsync()
    {
        // Redis TestContainer 시작
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _redisContainer.StartAsync();

        // Redis 캐시 인스턴스 생성
        var redisConfig = new RedisConfig
        {
            IpToNationConnectionString = _redisContainer.GetConnectionString(),
            KeyPrefix = "integration-test"
        };

        var mockOptions = new Mock<IOptions<RedisConfig>>();
        mockOptions.Setup(x => x.Value).Returns(redisConfig);

        _cache = new TestableIpToNationRedisCache(mockOptions.Object, _mockLogger.Object);
    }

    public async Task DisposeAsync()
    {
        if (_redisContainer != null)
        {
            await _redisContainer.StopAsync();
            await _redisContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task Cache_Should_Handle_Multiple_Concurrent_Operations()
    {
        // Arrange
        var concurrencyLevel = 20;
        var operations = new List<Task>();
        var ipCountryPairs = Enumerable.Range(1, concurrencyLevel)
            .Select(i => ($"192.168.1.{i}", _faker.Address.CountryCode()))
            .ToList();

        // Act - 동시에 설정
        foreach (var (ip, country) in ipCountryPairs)
        {
            operations.Add(_cache!.SetAsync(ip, country, TimeSpan.FromMinutes(10)));
        }
        await Task.WhenAll(operations);

        // Act - 동시에 조회
        var getTasks = ipCountryPairs.Select(async pair =>
        {
            var result = await _cache!.GetAsync(pair.Item1);
            return (IP: pair.Item1, Result: result);
        });

        var results = await Task.WhenAll(getTasks);

        // Assert
        results.Should().HaveCount(concurrencyLevel);
        results.Should().AllSatisfy(item =>
        {
            item.Result.IsSuccess.Should().BeTrue();
            item.Result.Value.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task Cache_Should_Handle_High_Throughput()
    {
        // Arrange
        var batchSize = 100; // 테스트 환경에서는 적당한 크기로 조정
        var tasks = new List<Task>();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 1; i <= batchSize; i++)
        {
            var ip = $"10.0.{i / 256}.{i % 256}";
            var country = _faker.Address.CountryCode();
            tasks.Add(_cache!.SetAsync(ip, country, TimeSpan.FromHours(1)));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        // 100개 항목을 설정하는데 5초 이내에 완료되어야 함
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);

        // 일부 항목들이 정상적으로 조회되는지 확인
        var testIps = new[] { "10.0.0.1", "10.0.0.10", "10.0.0.50" };
        foreach (var ip in testIps)
        {
            var result = await _cache!.GetAsync(ip);
            result.IsSuccess.Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("invalid-ip")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Cache_Should_Handle_Edge_Case_IP_Addresses(string edgeCaseIp)
    {
        // Arrange
        var countryCode = "XX";
        var expiry = TimeSpan.FromMinutes(10);

        // Act
        await _cache!.SetAsync(edgeCaseIp, countryCode, expiry);
        var result = await _cache.GetAsync(edgeCaseIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(countryCode);
    }

    [Fact]
    public async Task Cache_Should_Handle_Unicode_Country_Codes()
    {
        // Arrange
        var clientIp = _faker.Internet.Ip();
        var unicodeCountryCode = "🇰🇷-대한민국-KR";
        var expiry = TimeSpan.FromMinutes(10);

        // Act
        await _cache!.SetAsync(clientIp, unicodeCountryCode, expiry);
        var result = await _cache.GetAsync(clientIp);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(unicodeCountryCode);
    }
}