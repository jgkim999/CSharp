using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Demo.Application.Configs;
using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Demo.Infra.Tests.Fixtures;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;
using Testcontainers.Redis;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Tests.Benchmarks;

/// <summary>
/// IP 국가 코드 캐시 구현체들의 응답 시간 벤치마크 테스트
/// L1 캐시 히트, L2 캐시 히트, 기존 구현체와의 성능 비교를 수행합니다
/// Valkey(Redis 호환 오픈소스 포크)를 백엔드로 사용합니다
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[Config(typeof(Config))]
public class IpToNationCacheResponseTimeBenchmark : IAsyncDisposable
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default
                .WithWarmupCount(3)
                .WithIterationCount(10)
                .WithInvocationCount(1000));
        }
    }

    private RedisContainer? _redisContainer;
    private IpToNationFusionCache? _fusionCache;
    private IpToNationRedisCache? _redisCache;
    private IFusionCache? _fusionCacheInstance;
    private ConnectionMultiplexer? _connectionMultiplexer;
    
    // 테스트 데이터
    private readonly string[] _testIps = 
    {
        "192.168.1.1",
        "10.0.0.1", 
        "172.16.0.1",
        "203.104.144.1",
        "8.8.8.8"
    };
    
    private readonly string[] _testCountryCodes = { "KR", "US", "JP", "CN", "DE" };

    /// <summary>
    /// 벤치마크 테스트 환경을 설정합니다
    /// 공유 Valkey 컨테이너를 사용하고 캐시 구현체들을 초기화합니다
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // 공유 Redis 컨테이너 사용
        _redisContainer = await BenchmarkContainerFixture.GetRedisContainerAsync();
        var connectionString = BenchmarkContainerFixture.GetRedisConnectionString();
        
        // 서비스 컬렉션 설정
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Redis 설정
        var redisConfig = new RedisConfig
        {
            IpToNationConnectionString = connectionString,
            KeyPrefix = "benchmark"
        };
        services.Configure<RedisConfig>(config =>
        {
            config.IpToNationConnectionString = redisConfig.IpToNationConnectionString;
            config.KeyPrefix = redisConfig.KeyPrefix;
        });
        
        // FusionCache 설정
        services.Configure<FusionCacheConfig>(config =>
        {
            config.Redis = redisConfig;
            config.DefaultEntryOptions = TimeSpan.FromMinutes(30);
            config.L1CacheDuration = TimeSpan.FromMinutes(5);
            config.SoftTimeout = TimeSpan.FromSeconds(1);
            config.HardTimeout = TimeSpan.FromSeconds(5);
            config.EnableFailSafe = true;
            config.EnableEagerRefresh = true;
            config.EnableDetailedLogging = false;
        });
        
        // Redis 연결 설정
        _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        services.AddSingleton(_connectionMultiplexer);
        
        // IDistributedCache 등록 (Redis 기반)
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "BenchmarkInstance";
        });
        
        // FusionCache 등록
        services.AddSingleton<IMemoryCache, MemoryCache>();
        services.AddSingleton<IFusionCache>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FusionCache>>();
            var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            
            return new FusionCache(new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = TimeSpan.FromMinutes(30),
                    Priority = CacheItemPriority.Normal,
                    Size = 1,
                    FailSafeMaxDuration = TimeSpan.FromHours(1),
                    FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
                }
            }, memoryCache, logger)
            .SetupDistributedCache(distributedCache);
        });
        
        // 메트릭 서비스 등록
        services.AddSingleton<FusionCacheMetricsService>();
        
        // 캐시 구현체들 등록
        services.AddSingleton<IpToNationFusionCache>();
        services.AddSingleton<IpToNationRedisCache>(serviceProvider =>
        {
            var redisConfigOptions = serviceProvider.GetRequiredService<IOptions<RedisConfig>>();
            var logger = serviceProvider.GetRequiredService<ILogger<IpToNationRedisCache>>();
            var mockInstrumentation = new Mock<StackExchangeRedisInstrumentation>();
            return new IpToNationRedisCache(redisConfigOptions, logger, mockInstrumentation.Object);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        // 캐시 인스턴스 가져오기
        _fusionCache = serviceProvider.GetRequiredService<IpToNationFusionCache>();
        _redisCache = serviceProvider.GetRequiredService<IpToNationRedisCache>();
        _fusionCacheInstance = serviceProvider.GetRequiredService<IFusionCache>();
        
        // 테스트 데이터 사전 로드 (L2 캐시에만)
        await PreloadTestData();
    }

    /// <summary>
    /// 테스트 데이터를 사전 로드합니다
    /// L2 캐시(Redis)에 데이터를 저장하여 L2 히트 시나리오를 준비합니다
    /// </summary>
    private async Task PreloadTestData()
    {
        // Redis에 직접 데이터 저장 (L2 캐시 히트 시나리오용)
        var database = _connectionMultiplexer!.GetDatabase();
        
        for (int i = 0; i < _testIps.Length; i++)
        {
            var key = $"benchmark:ipcache:{_testIps[i]}";
            await database.StringSetAsync(key, _testCountryCodes[i], TimeSpan.FromMinutes(30));
        }
        
        // L1 캐시 히트 시나리오를 위해 FusionCache에 일부 데이터 로드
        for (int i = 0; i < 2; i++) // 처음 2개만 L1에 로드
        {
            await _fusionCache!.GetAsync(_testIps[i]);
        }
    }

    /// <summary>
    /// L1 캐시 히트 시나리오 벤치마크
    /// 메모리 캐시에서 직접 조회하는 가장 빠른 시나리오입니다
    /// </summary>
    [Benchmark(Description = "FusionCache L1 Cache Hit")]
    public async Task<string?> FusionCache_L1_Hit()
    {
        var result = await _fusionCache!.GetAsync(_testIps[0]); // L1에 로드된 데이터
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// L2 캐시 히트 시나리오 벤치마크
    /// L1 캐시 미스 후 Redis에서 조회하는 시나리오입니다
    /// </summary>
    [Benchmark(Description = "FusionCache L2 Cache Hit")]
    public async Task<string?> FusionCache_L2_Hit()
    {
        // L1 캐시를 우회하기 위해 새로운 IP 사용
        var testIp = _testIps[2]; // L1에 없지만 L2에 있는 데이터
        
        // L1 캐시에서 제거 (L2 히트 시나리오 보장)
        await _fusionCacheInstance!.RemoveAsync($"benchmark:ipcache:{testIp}");
        
        var result = await _fusionCache!.GetAsync(testIp);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// 기존 Redis 구현체 벤치마크
    /// 직접 Redis 조회를 수행하는 기존 구현체의 성능을 측정합니다
    /// </summary>
    [Benchmark(Description = "Redis Cache Direct")]
    public async Task<string?> RedisCache_Direct()
    {
        var result = await _redisCache!.GetAsync(_testIps[3]);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// FusionCache 캐시 미스 시나리오 벤치마크
    /// 캐시에 없는 데이터를 조회하는 시나리오입니다
    /// </summary>
    [Benchmark(Description = "FusionCache Cache Miss")]
    public async Task<string?> FusionCache_Miss()
    {
        var nonExistentIp = $"999.999.999.{DateTime.Now.Ticks % 255}";
        var result = await _fusionCache!.GetAsync(nonExistentIp);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// 기존 Redis 구현체 캐시 미스 시나리오 벤치마크
    /// </summary>
    [Benchmark(Description = "Redis Cache Miss")]
    public async Task<string?> RedisCache_Miss()
    {
        var nonExistentIp = $"888.888.888.{DateTime.Now.Ticks % 255}";
        var result = await _redisCache!.GetAsync(nonExistentIp);
        return result.IsSuccess ? result.Value : null;
    }

    /// <summary>
    /// FusionCache 캐시 설정 작업 벤치마크
    /// </summary>
    [Benchmark(Description = "FusionCache Set Operation")]
    public async Task FusionCache_Set()
    {
        var testIp = $"192.168.100.{DateTime.Now.Ticks % 255}";
        await _fusionCache!.SetAsync(testIp, "KR", TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 기존 Redis 구현체 캐시 설정 작업 벤치마크
    /// </summary>
    [Benchmark(Description = "Redis Cache Set Operation")]
    public async Task RedisCache_Set()
    {
        var testIp = $"10.0.100.{DateTime.Now.Ticks % 255}";
        await _redisCache!.SetAsync(testIp, "US", TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        await DisposeAsync();
    }

    /// <summary>
    /// 비동기 리소스 해제
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _connectionMultiplexer?.Dispose();

        // 공유 컨테이너 참조 해제 (실제 정리는 모든 벤치마크가 종료된 후)
        await BenchmarkContainerFixture.ReleaseAsync();

        GC.SuppressFinalize(this);
    }
}