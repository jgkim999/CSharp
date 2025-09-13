using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Demo.Application.Configs;
using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Diagnostics;
using Testcontainers.Redis;
using ZiggyCreatures.Caching.Fusion;

namespace Demo.Infra.Benchmarks;

/// <summary>
/// IP 국가 코드 캐시 구현체들의 동시성 및 처리량 벤치마크 테스트
/// 동시 요청 처리 능력, 캐시 스탬피드 방지 효과, 메모리 사용량을 측정합니다
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[Config(typeof(Config))]
public class IpToNationCacheConcurrencyBenchmark : IAsyncDisposable
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default
                .WithWarmupCount(2)
                .WithIterationCount(5)
                .WithInvocationCount(100));
        }
    }

    private RedisContainer? _redisContainer;
    private IpToNationFusionCache? _fusionCache;
    private IpToNationRedisCache? _redisCache;
    private IFusionCache? _fusionCacheInstance;
    private ConnectionMultiplexer? _connectionMultiplexer;
    
    // 동시성 테스트용 데이터
    private readonly string[] _concurrentTestIps = 
    {
        "192.168.1.100", "192.168.1.101", "192.168.1.102", "192.168.1.103", "192.168.1.104",
        "10.0.0.100", "10.0.0.101", "10.0.0.102", "10.0.0.103", "10.0.0.104",
        "172.16.0.100", "172.16.0.101", "172.16.0.102", "172.16.0.103", "172.16.0.104"
    };
    
    private readonly string[] _countryCodes = { "KR", "US", "JP", "CN", "DE", "FR", "GB", "CA", "AU", "IN" };

    // 캐시 스탬피드 테스트용 단일 IP
    private const string StampedeTestIp = "203.104.144.100";

    /// <summary>
    /// 벤치마크 테스트 환경을 설정합니다
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Redis 컨테이너 시작
        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .WithPortBinding(6379, true)
            .Build();
        
        await _redisContainer.StartAsync();
        
        var connectionString = _redisContainer.GetConnectionString();
        
        // 서비스 컬렉션 설정
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        
        // Redis 설정
        var redisConfig = new RedisConfig
        {
            IpToNationConnectionString = connectionString,
            KeyPrefix = "concurrency"
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
            options.InstanceName = "ConcurrencyBenchmark";
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
        
        // 테스트 데이터 사전 로드
        await PreloadConcurrencyTestData();
    }

    /// <summary>
    /// 동시성 테스트를 위한 데이터를 사전 로드합니다
    /// </summary>
    private async Task PreloadConcurrencyTestData()
    {
        // Redis에 테스트 데이터 저장
        var database = _connectionMultiplexer!.GetDatabase();
        
        for (int i = 0; i < _concurrentTestIps.Length; i++)
        {
            var key = $"concurrency:ipcache:{_concurrentTestIps[i]}";
            var countryCode = _countryCodes[i % _countryCodes.Length];
            await database.StringSetAsync(key, countryCode, TimeSpan.FromMinutes(30));
        }
        
        // 캐시 스탬피드 테스트용 데이터 저장
        var stampedeKey = $"concurrency:ipcache:{StampedeTestIp}";
        await database.StringSetAsync(stampedeKey, "KR", TimeSpan.FromMinutes(30));
    }

    /// <summary>
    /// FusionCache 동시 읽기 처리량 테스트 (10개 동시 요청)
    /// </summary>
    [Benchmark(Description = "FusionCache Concurrent Reads (10 threads)")]
    public async Task FusionCache_ConcurrentReads_10()
    {
        var tasks = new Task[10];
        
        for (int i = 0; i < 10; i++)
        {
            var ipIndex = i % _concurrentTestIps.Length;
            var testIp = _concurrentTestIps[ipIndex];
            
            tasks[i] = Task.Run(async () =>
            {
                await _fusionCache!.GetAsync(testIp);
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 기존 Redis 구현체 동시 읽기 처리량 테스트 (10개 동시 요청)
    /// </summary>
    [Benchmark(Description = "Redis Cache Concurrent Reads (10 threads)")]
    public async Task RedisCache_ConcurrentReads_10()
    {
        var tasks = new Task[10];
        
        for (int i = 0; i < 10; i++)
        {
            var ipIndex = i % _concurrentTestIps.Length;
            var testIp = _concurrentTestIps[ipIndex];
            
            tasks[i] = Task.Run(async () =>
            {
                await _redisCache!.GetAsync(testIp);
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// FusionCache 동시 읽기 처리량 테스트 (50개 동시 요청)
    /// </summary>
    [Benchmark(Description = "FusionCache Concurrent Reads (50 threads)")]
    public async Task FusionCache_ConcurrentReads_50()
    {
        var tasks = new Task[50];
        
        for (int i = 0; i < 50; i++)
        {
            var ipIndex = i % _concurrentTestIps.Length;
            var testIp = _concurrentTestIps[ipIndex];
            
            tasks[i] = Task.Run(async () =>
            {
                await _fusionCache!.GetAsync(testIp);
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 기존 Redis 구현체 동시 읽기 처리량 테스트 (50개 동시 요청)
    /// </summary>
    [Benchmark(Description = "Redis Cache Concurrent Reads (50 threads)")]
    public async Task RedisCache_ConcurrentReads_50()
    {
        var tasks = new Task[50];
        
        for (int i = 0; i < 50; i++)
        {
            var ipIndex = i % _concurrentTestIps.Length;
            var testIp = _concurrentTestIps[ipIndex];
            
            tasks[i] = Task.Run(async () =>
            {
                await _redisCache!.GetAsync(testIp);
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// FusionCache 캐시 스탬피드 방지 효과 테스트
    /// 동일한 키에 대한 동시 요청이 중복 처리되지 않는지 확인합니다
    /// </summary>
    [Benchmark(Description = "FusionCache Cache Stampede Prevention")]
    public async Task FusionCache_CacheStampedePrevention()
    {
        // L1 캐시에서 제거하여 L2에서 가져오도록 함
        await _fusionCacheInstance!.RemoveAsync($"concurrency:ipcache:{StampedeTestIp}");
        
        var tasks = new Task[20];
        var results = new ConcurrentBag<string>();
        var requestCounts = new ConcurrentDictionary<string, int>();
        
        // 동일한 키에 대한 20개의 동시 요청
        for (int i = 0; i < 20; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId.ToString();
                requestCounts.AddOrUpdate(threadId, 1, (key, value) => value + 1);
                
                var result = await _fusionCache!.GetAsync(StampedeTestIp);
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 기존 Redis 구현체 동일 키 동시 요청 테스트 (비교용)
    /// </summary>
    [Benchmark(Description = "Redis Cache Same Key Concurrent Requests")]
    public async Task RedisCache_SameKeyConcurrentRequests()
    {
        var tasks = new Task[20];
        var results = new ConcurrentBag<string>();
        
        // 동일한 키에 대한 20개의 동시 요청
        for (int i = 0; i < 20; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                var result = await _redisCache!.GetAsync(StampedeTestIp);
                if (result.IsSuccess)
                {
                    results.Add(result.Value);
                }
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// FusionCache 혼합 작업 (읽기/쓰기) 처리량 테스트
    /// </summary>
    [Benchmark(Description = "FusionCache Mixed Operations (Read/Write)")]
    public async Task FusionCache_MixedOperations()
    {
        var tasks = new Task[20];
        
        for (int i = 0; i < 20; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                if (index % 3 == 0) // 쓰기 작업 (33%)
                {
                    var testIp = $"192.168.200.{index}";
                    var countryCode = _countryCodes[index % _countryCodes.Length];
                    await _fusionCache!.SetAsync(testIp, countryCode, TimeSpan.FromMinutes(5));
                }
                else // 읽기 작업 (67%)
                {
                    var ipIndex = index % _concurrentTestIps.Length;
                    var testIp = _concurrentTestIps[ipIndex];
                    await _fusionCache!.GetAsync(testIp);
                }
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 기존 Redis 구현체 혼합 작업 (읽기/쓰기) 처리량 테스트
    /// </summary>
    [Benchmark(Description = "Redis Cache Mixed Operations (Read/Write)")]
    public async Task RedisCache_MixedOperations()
    {
        var tasks = new Task[20];
        
        for (int i = 0; i < 20; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                if (index % 3 == 0) // 쓰기 작업 (33%)
                {
                    var testIp = $"10.0.200.{index}";
                    var countryCode = _countryCodes[index % _countryCodes.Length];
                    await _redisCache!.SetAsync(testIp, countryCode, TimeSpan.FromMinutes(5));
                }
                else // 읽기 작업 (67%)
                {
                    var ipIndex = index % _concurrentTestIps.Length;
                    var testIp = _concurrentTestIps[ipIndex];
                    await _redisCache!.GetAsync(testIp);
                }
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// FusionCache 메모리 사용량 모니터링 테스트
    /// 대량의 캐시 항목을 생성하여 메모리 사용 패턴을 확인합니다
    /// </summary>
    [Benchmark(Description = "FusionCache Memory Usage Test")]
    public async Task FusionCache_MemoryUsageTest()
    {
        var tasks = new Task[100];
        
        // 100개의 새로운 캐시 항목 생성
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var testIp = $"172.16.100.{index}";
                var countryCode = _countryCodes[index % _countryCodes.Length];
                await _fusionCache!.SetAsync(testIp, countryCode, TimeSpan.FromMinutes(1));
                
                // 설정 후 즉시 읽기
                await _fusionCache!.GetAsync(testIp);
            });
        }
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 기존 Redis 구현체 메모리 사용량 비교 테스트
    /// </summary>
    [Benchmark(Description = "Redis Cache Memory Usage Test")]
    public async Task RedisCache_MemoryUsageTest()
    {
        var tasks = new Task[100];
        
        // 100개의 새로운 캐시 항목 생성
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var testIp = $"203.104.100.{index}";
                var countryCode = _countryCodes[index % _countryCodes.Length];
                await _redisCache!.SetAsync(testIp, countryCode, TimeSpan.FromMinutes(1));
                
                // 설정 후 즉시 읽기
                await _redisCache!.GetAsync(testIp);
            });
        }
        
        await Task.WhenAll(tasks);
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
        
        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }
        
        GC.SuppressFinalize(this);
    }
}