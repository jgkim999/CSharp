using Demo.Application.Configs;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;

namespace Demo.Infra.Tests.Repositories;

/// <summary>
/// IpToNationFusionCache와 기존 Redis 데이터 간의 호환성을 검증하는 통합 테스트
/// 실제 Redis 인스턴스가 필요하므로 CI/CD 환경에서는 조건부로 실행됩니다
/// </summary>
public class IpToNationFusionCacheIntegrationTests : IDisposable
{
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly IDatabase? _database;
    private readonly IpToNationFusionCache? _fusionCache;
    private readonly IpToNationRedisCache? _redisCache;
    private readonly string _testKeyPrefix = "integration_test";
    private readonly bool _isRedisAvailable;

    public IpToNationFusionCacheIntegrationTests()
    {
        try
        {
            // Redis 연결 시도 (테스트 환경에서 Redis가 사용 가능한 경우에만)
            var connectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") 
                                 ?? "localhost:6379";

            _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _database = _connectionMultiplexer.GetDatabase();
            
            // 연결 테스트
            _database.Ping();
            _isRedisAvailable = true;

            // 테스트용 설정 생성
            var redisConfig = new RedisConfig
            {
                IpToNationConnectionString = connectionString,
                KeyPrefix = _testKeyPrefix
            };

            var fusionCacheConfig = new FusionCacheConfig
            {
                Redis = redisConfig,
                DefaultEntryOptions = TimeSpan.FromMinutes(30),
                L1CacheDuration = TimeSpan.FromMinutes(5),
                EnableFailSafe = true,
                EnableEagerRefresh = false // 테스트에서는 비활성화
            };

            // 서비스 컬렉션 설정
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddMemoryCache();
            
            // IDistributedCache 설정
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = connectionString;
                options.InstanceName = $"{_testKeyPrefix}:IpToNationCache";
            });

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<IpToNationFusionCache>>();
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();

            // FusionCache 인스턴스 생성
            var fusionCacheOptions = new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = fusionCacheConfig.DefaultEntryOptions,
                    IsFailSafeEnabled = fusionCacheConfig.EnableFailSafe
                },
                CacheName = $"{_testKeyPrefix}:IpToNationCache"
            };

            var fusionCacheInstance = new FusionCache(fusionCacheOptions, memoryCache, 
                serviceProvider.GetRequiredService<ILogger<FusionCache>>());
            
            var serializer = new FusionCacheSystemTextJsonSerializer();
            fusionCacheInstance.SetupDistributedCache(distributedCache, serializer);

            // 캐시 구현체들 생성
            _fusionCache = new IpToNationFusionCache(
                fusionCacheInstance,
                Options.Create(fusionCacheConfig),
                logger);

            var redisLogger = serviceProvider.GetRequiredService<ILogger<IpToNationRedisCache>>();
            // Redis 인스트루멘테이션은 테스트에서 생략
            // _redisCache = new IpToNationRedisCache(
            //     Options.Create(redisConfig),
            //     redisLogger,
            //     null); // 인스트루멘테이션 생략
        }
        catch (Exception)
        {
            _isRedisAvailable = false;
        }
    }

    /// <summary>
    /// Redis가 사용 가능한 환경에서만 테스트를 실행하는 헬퍼 메서드
    /// </summary>
    private void SkipIfRedisNotAvailable()
    {
        if (!_isRedisAvailable)
        {
            // xUnit에서 테스트 스킵
            throw new SkipException("Redis가 사용 가능하지 않아 통합 테스트를 건너뜁니다");
        }
    }

    /// <summary>
    /// 기존 Redis 데이터를 FusionCache로 읽을 수 있는지 검증하는 테스트
    /// 요구사항 6.4 검증
    /// </summary>
    [Fact]
    public async Task FusionCache_ShouldReadExistingRedisData()
    {
        SkipIfRedisNotAvailable();

        // Arrange
        const string testIp = "192.168.100.1";
        const string testCountryCode = "KR";
        var expectedKey = $"{_testKeyPrefix}:ipcache:{testIp}";

        // 기존 Redis 방식으로 데이터 저장
        await _database!.StringSetAsync(expectedKey, testCountryCode, TimeSpan.FromMinutes(30));

        // Act - FusionCache로 데이터 읽기
        var result = await _fusionCache!.GetAsync(testIp);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(testCountryCode, result.Value);

        // Cleanup
        await _database.KeyDeleteAsync(expectedKey);
    }

    /// <summary>
    /// FusionCache로 저장한 데이터를 기존 Redis 방식으로 읽을 수 있는지 검증하는 테스트
    /// 요구사항 6.4 검증
    /// </summary>
    [Fact]
    public async Task RedisDirectAccess_ShouldReadFusionCacheData()
    {
        SkipIfRedisNotAvailable();

        // Arrange
        const string testIp = "10.0.100.2";
        const string testCountryCode = "US";
        var expectedKey = $"{_testKeyPrefix}:ipcache:{testIp}";

        // Act - FusionCache로 데이터 저장
        await _fusionCache!.SetAsync(testIp, testCountryCode, TimeSpan.FromMinutes(30));

        // 잠시 대기하여 L2 캐시에 저장되도록 함
        await Task.Delay(100);

        // Redis에서 직접 데이터 읽기
        var redisValue = await _database!.StringGetAsync(expectedKey);

        // Assert
        Assert.True(redisValue.HasValue);
        Assert.Equal(testCountryCode, redisValue.ToString());

        // Cleanup
        await _database.KeyDeleteAsync(expectedKey);
    }

    /// <summary>
    /// 키 접두사가 올바르게 적용되어 기존 데이터와 호환되는지 검증하는 테스트
    /// 요구사항 1.3, 6.2 검증
    /// </summary>
    [Theory]
    [InlineData("172.16.1.1", "JP")]
    [InlineData("203.0.113.5", "CN")]
    [InlineData("198.51.100.10", "DE")]
    public async Task KeyPrefix_ShouldMaintainCompatibilityWithExistingData(string testIp, string testCountryCode)
    {
        SkipIfRedisNotAvailable();

        // Arrange
        var expectedKey = $"{_testKeyPrefix}:ipcache:{testIp}";

        // 기존 방식으로 데이터 저장
        await _database!.StringSetAsync(expectedKey, testCountryCode, TimeSpan.FromMinutes(30));

        // Act & Assert - FusionCache로 읽기
        var fusionResult = await _fusionCache!.GetAsync(testIp);
        Assert.True(fusionResult.IsSuccess);
        Assert.Equal(testCountryCode, fusionResult.Value);

        // FusionCache로 업데이트
        const string updatedCountryCode = "XX";
        await _fusionCache.SetAsync(testIp, updatedCountryCode, TimeSpan.FromMinutes(30));

        // 잠시 대기
        await Task.Delay(100);

        // Redis에서 직접 확인
        var redisValue = await _database.StringGetAsync(expectedKey);
        Assert.True(redisValue.HasValue);
        Assert.Equal(updatedCountryCode, redisValue.ToString());

        // Cleanup
        await _database.KeyDeleteAsync(expectedKey);
    }

    /// <summary>
    /// L1과 L2 캐시 계층이 올바르게 작동하는지 검증하는 테스트
    /// 요구사항 2.1, 2.2, 2.4 검증
    /// </summary>
    [Fact]
    public async Task CacheHierarchy_ShouldWorkCorrectly()
    {
        SkipIfRedisNotAvailable();

        // Arrange
        const string testIp = "192.168.200.1";
        const string testCountryCode = "FR";

        // Act - 첫 번째 설정 (L1과 L2에 모두 저장됨)
        await _fusionCache!.SetAsync(testIp, testCountryCode, TimeSpan.FromMinutes(30));

        // 첫 번째 읽기 (L1에서 히트)
        var firstRead = await _fusionCache.GetAsync(testIp);
        Assert.True(firstRead.IsSuccess);
        Assert.Equal(testCountryCode, firstRead.Value);

        // L1 캐시 클리어 시뮬레이션을 위해 새로운 FusionCache 인스턴스 생성
        // (실제로는 메모리 캐시만 초기화됨)
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddMemoryCache(); // 새로운 메모리 캐시

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = _connectionMultiplexer!.Configuration;
            options.InstanceName = $"{_testKeyPrefix}:IpToNationCache";
        });

        var serviceProvider = services.BuildServiceProvider();
        var newMemoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
        var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();

        var fusionCacheOptions = new FusionCacheOptions
        {
            DefaultEntryOptions = new FusionCacheEntryOptions
            {
                Duration = TimeSpan.FromMinutes(30),
                IsFailSafeEnabled = true
            },
            CacheName = $"{_testKeyPrefix}:IpToNationCache"
        };

        var newFusionCacheInstance = new FusionCache(fusionCacheOptions, newMemoryCache,
            serviceProvider.GetRequiredService<ILogger<FusionCache>>());

        var serializer = new FusionCacheSystemTextJsonSerializer();
        newFusionCacheInstance.SetupDistributedCache(distributedCache, serializer);

        var fusionCacheConfig = new FusionCacheConfig
        {
            Redis = new RedisConfig { KeyPrefix = _testKeyPrefix }
        };

        var newFusionCache = new IpToNationFusionCache(
            newFusionCacheInstance,
            Options.Create(fusionCacheConfig),
            serviceProvider.GetRequiredService<ILogger<IpToNationFusionCache>>());

        // 두 번째 읽기 (L1 미스, L2에서 히트)
        var secondRead = await newFusionCache.GetAsync(testIp);
        Assert.True(secondRead.IsSuccess);
        Assert.Equal(testCountryCode, secondRead.Value);

        // Cleanup
        var expectedKey = $"{_testKeyPrefix}:ipcache:{testIp}";
        await _database!.KeyDeleteAsync(expectedKey);
    }

    public void Dispose()
    {
        _connectionMultiplexer?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 테스트 스킵을 위한 예외 클래스
/// </summary>
public class SkipException : Exception
{
    public SkipException(string message) : base(message) { }
}