using Demo.Application.Configs;
using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Repositories;
using Demo.Infra.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Serialization.SystemTextJson;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Demo.Infra.Extensions;

/// <summary>
/// IServiceCollection에 대한 확장 메서드를 제공합니다
/// FusionCache 및 관련 서비스 등록을 위한 메서드들을 포함합니다
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// IpToNationFusionCache와 관련된 모든 서비스를 등록합니다
    /// L1(메모리) + L2(Redis) 하이브리드 캐시 구조를 설정합니다
    /// 기존 RedisConfig와 호환성을 유지하면서 FusionCache 기능을 제공합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">애플리케이션 구성</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddIpToNationFusionCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 기존 RedisConfig 등록 (호환성 유지)
        services.Configure<RedisConfig>(
            configuration.GetSection("Redis"));

        // FusionCacheConfig 등록 및 유효성 검증 활성화
        services.AddValidatedFusionCacheConfig(configuration);

        // FusionCacheConfig와 RedisConfig 통합 설정
        services.Configure<FusionCacheConfig>(fusionCacheConfig =>
        {
            // FusionCache 전용 설정 바인딩
            configuration.GetSection("FusionCache").Bind(fusionCacheConfig);
            
            // RedisConfig와 통합
            var redisConfig = new RedisConfig();
            configuration.GetSection("Redis").Bind(redisConfig);
            fusionCacheConfig.Redis = redisConfig;
        });

        // Redis 연결 설정 및 IDistributedCache 등록
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisConfig = serviceProvider.GetRequiredService<IOptions<RedisConfig>>().Value;
            var connectionString = redisConfig.IpToNationConnectionString;
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Redis IpToNationConnectionString이 설정되지 않았습니다. appsettings.json에서 Redis:IpToNationConnectionString을 확인해주세요.");
            }

            return ConnectionMultiplexer.Connect(connectionString);
        });

        // IDistributedCache로 Redis 구현체 등록 (기존 RedisConfig 사용)
        services.AddStackExchangeRedisCache(options =>
        {
            var redisConfig = configuration.GetSection("Redis").Get<RedisConfig>();
            if (redisConfig == null || string.IsNullOrEmpty(redisConfig.IpToNationConnectionString))
            {
                throw new InvalidOperationException(
                    "Redis 설정이 올바르지 않습니다. appsettings.json에서 Redis 섹션을 확인해주세요.");
            }

            options.Configuration = redisConfig.IpToNationConnectionString;
            options.InstanceName = string.IsNullOrEmpty(redisConfig.KeyPrefix) 
                ? "IpToNationCache" 
                : $"{redisConfig.KeyPrefix}:IpToNationCache";
        });

        // L1 메모리 캐시 설정
        services.AddMemoryCache(options =>
        {
            var serviceProvider = services.BuildServiceProvider();
            var fusionCacheConfig = serviceProvider.GetRequiredService<IOptions<FusionCacheConfig>>().Value;
            
            options.SizeLimit = fusionCacheConfig.L1CacheMaxSize;
            options.CompactionPercentage = 0.25; // 메모리 압박 시 25% 제거
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(1); // 만료된 항목 스캔 주기
        });

        // FusionCache 인스턴스를 싱글톤으로 등록
        services.AddSingleton<IFusionCache>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<FusionCache>>();
            var fusionCacheConfig = serviceProvider.GetRequiredService<IOptions<FusionCacheConfig>>().Value;
            var distributedCache = serviceProvider.GetRequiredService<IDistributedCache>();
            var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();

            // 캐시 이름에 키 접두사 적용 (기존 RedisConfig와 호환성 유지)
            var cacheName = string.IsNullOrEmpty(fusionCacheConfig.KeyPrefix) 
                ? "IpToNationCache" 
                : $"{fusionCacheConfig.KeyPrefix}:IpToNationCache";

            // FusionCache 옵션 구성
            var options = new FusionCacheOptions
            {
                DefaultEntryOptions = new FusionCacheEntryOptions
                {
                    Duration = fusionCacheConfig.DefaultEntryOptions,
                    Priority = CacheItemPriority.Normal,
                    Size = 1,
                    FailSafeMaxDuration = fusionCacheConfig.FailSafeMaxDuration,
                    FailSafeThrottleDuration = fusionCacheConfig.FailSafeThrottleDuration,
                    IsFailSafeEnabled = fusionCacheConfig.EnableFailSafe,
                    EagerRefreshThreshold = fusionCacheConfig.EagerRefreshThreshold,
                    
                    // 백그라운드 새로고침 설정
                    AllowBackgroundDistributedCacheOperations = true,
                    ReThrowDistributedCacheExceptions = false,
                    
                    // 타임아웃 설정
                    DistributedCacheSoftTimeout = fusionCacheConfig.SoftTimeout,
                    DistributedCacheHardTimeout = fusionCacheConfig.HardTimeout,
                    
                    // 캐시 스탬피드 방지
                    AllowTimedOutFactoryBackgroundCompletion = fusionCacheConfig.EnableCacheStampedeProtection
                },
                CacheName = cacheName,
                EnableSyncEventHandlersExecution = false
            };

            // FusionCache 인스턴스 생성
            var fusionCache = new FusionCache(options, memoryCache, logger);

            // L2 캐시 (Redis) 설정
            if (distributedCache != null)
            {
                var serializer = new FusionCacheSystemTextJsonSerializer();
                fusionCache.SetupDistributedCache(distributedCache, serializer);
                
                logger.LogInformation("FusionCache L2 (Redis) 캐시가 성공적으로 설정되었습니다. KeyPrefix: {KeyPrefix}", 
                    fusionCacheConfig.KeyPrefix);
            }
            else
            {
                logger.LogWarning("FusionCache L2 (Redis) 캐시를 사용할 수 없습니다. L1 메모리 캐시만 사용됩니다");
            }

            // 이벤트 핸들러 등록 (로깅 및 모니터링용)
            if (fusionCacheConfig.EnableDetailedLogging || fusionCacheConfig.EnableMetrics)
            {
                RegisterEventHandlers(fusionCache, logger, fusionCacheConfig);
            }

            // OpenTelemetry 계측 설정
            if (fusionCacheConfig.EnableOpenTelemetry)
            {
                SetupOpenTelemetryInstrumentation(fusionCache, logger);
            }

            logger.LogInformation("FusionCache가 성공적으로 초기화되었습니다. CacheName: {CacheName}, ConnectionString: {ConnectionString}", 
                cacheName, fusionCacheConfig.ConnectionString?.AsSpan(0, Math.Min(20, fusionCacheConfig.ConnectionString.Length)).ToString() + "...");

            return fusionCache;
        });

        // FusionCache 메트릭 서비스 등록
        services.AddSingleton<FusionCacheMetricsService>();

        // 동적 설정 업데이트 서비스 등록
        services.AddSingleton<DynamicFusionCacheConfigService>();
        services.AddHostedService<DynamicFusionCacheConfigService>(provider => 
            provider.GetRequiredService<DynamicFusionCacheConfigService>());

        // 기존 Redis 캐시 구현체 등록 (전환 메커니즘을 위해 유지)
        services.AddScoped<IpToNationRedisCache>();
        
        // FusionCache 구현체 등록
        services.AddScoped<IpToNationFusionCache>();

        // IIpToNationCache 인터페이스에 FusionCache 구현체 등록
        services.AddScoped<IIpToNationCache, IpToNationFusionCache>();

        return services;
    }

    /// <summary>
    /// FusionCache OpenTelemetry 메트릭을 OpenTelemetry 서비스에 등록합니다
    /// 이 메서드는 애플리케이션의 OpenTelemetry 설정에서 호출되어야 합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddFusionCacheOpenTelemetry(this IServiceCollection services)
    {
        // FusionCache 메트릭을 위한 MeterProvider 설정은 
        // 애플리케이션의 OpenTelemetry 설정에서 수행됩니다
        // 여기서는 필요한 서비스만 등록합니다
        
        return services;
    }

    /// <summary>
    /// FusionCache 이벤트 핸들러를 등록합니다
    /// 캐시 작업에 대한 구조화된 로깅과 메트릭 수집을 제공합니다
    /// </summary>
    /// <param name="fusionCache">FusionCache 인스턴스</param>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="config">FusionCache 설정</param>
    private static void RegisterEventHandlers(FusionCache fusionCache, ILogger logger, FusionCacheConfig config)
    {
        var logLevel = config.CacheEventLogLevel;
        var enableDetailedLogging = config.EnableDetailedLogging;
        
        // 메트릭 수집을 위한 Meter 및 Instruments
        var meter = new Meter("Demo.Infra.FusionCache.Events", "1.0.0");
        var hitCounter = meter.CreateCounter<long>("fusion_cache_event_hits_total", 
            description: "FusionCache 이벤트 히트 횟수");
        var missCounter = meter.CreateCounter<long>("fusion_cache_event_misses_total", 
            description: "FusionCache 이벤트 미스 횟수");
        var setCounter = meter.CreateCounter<long>("fusion_cache_event_sets_total", 
            description: "FusionCache 이벤트 설정 횟수");
        var removeCounter = meter.CreateCounter<long>("fusion_cache_event_removes_total", 
            description: "FusionCache 이벤트 제거 횟수");
        var expireCounter = meter.CreateCounter<long>("fusion_cache_event_expires_total", 
            description: "FusionCache 이벤트 만료 횟수");
        var failsafeCounter = meter.CreateCounter<long>("fusion_cache_event_failsafe_activations_total", 
            description: "FusionCache 이벤트 페일세이프 활성화 횟수");
        var factoryErrorCounter = meter.CreateCounter<long>("fusion_cache_event_factory_errors_total", 
            description: "FusionCache 이벤트 팩토리 오류 횟수");
        var backgroundSuccessCounter = meter.CreateCounter<long>("fusion_cache_event_background_success_total", 
            description: "FusionCache 이벤트 백그라운드 성공 횟수");
        var backgroundErrorCounter = meter.CreateCounter<long>("fusion_cache_event_background_errors_total", 
            description: "FusionCache 이벤트 백그라운드 오류 횟수");

        fusionCache.Events.Hit += (sender, e) =>
        {
            hitCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (logger.IsEnabled(logLevel))
            {
                if (enableDetailedLogging)
                {
                    logger.Log(logLevel, "FusionCache Hit: KeyHash={KeyHash}, CacheName={CacheName}, " +
                        "IsStale={IsStale}, Timestamp={Timestamp}", 
                        e.Key.GetHashCode(), fusionCache.CacheName, e.IsStale, DateTimeOffset.UtcNow);
                }
                else
                {
                    logger.Log(logLevel, "FusionCache Hit: Key={Key}", e.Key);
                }
            }
        };

        fusionCache.Events.Miss += (sender, e) =>
        {
            missCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (logger.IsEnabled(logLevel))
            {
                if (enableDetailedLogging)
                {
                    logger.Log(logLevel, "FusionCache Miss: KeyHash={KeyHash}, CacheName={CacheName}, " +
                        "Timestamp={Timestamp}", 
                        e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
                }
                else
                {
                    logger.Log(logLevel, "FusionCache Miss: Key={Key}", e.Key);
                }
            }
        };

        fusionCache.Events.Set += (sender, e) =>
        {
            setCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (logger.IsEnabled(logLevel))
            {
                if (enableDetailedLogging)
                {
                    logger.Log(logLevel, "FusionCache Set: KeyHash={KeyHash}, CacheName={CacheName}, " +
                        "Timestamp={Timestamp}", 
                        e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
                }
                else
                {
                    logger.Log(logLevel, "FusionCache Set: Key={Key}", e.Key);
                }
            }
        };

        fusionCache.Events.Remove += (sender, e) =>
        {
            removeCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (logger.IsEnabled(logLevel))
            {
                if (enableDetailedLogging)
                {
                    logger.Log(logLevel, "FusionCache Remove: KeyHash={KeyHash}, CacheName={CacheName}, " +
                        "Timestamp={Timestamp}", 
                        e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
                }
                else
                {
                    logger.Log(logLevel, "FusionCache Remove: Key={Key}", e.Key);
                }
            }
        };

        fusionCache.Events.Expire += (sender, e) =>
        {
            expireCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (logger.IsEnabled(LogLevel.Information))
            {
                if (enableDetailedLogging)
                {
                    logger.LogInformation("FusionCache Expire: KeyHash={KeyHash}, CacheName={CacheName}, " +
                        "Timestamp={Timestamp}", 
                        e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
                }
                else
                {
                    logger.LogInformation("FusionCache Expire: Key={Key}", e.Key);
                }
            }
        };

        fusionCache.Events.FailSafeActivate += (sender, e) =>
        {
            failsafeCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (enableDetailedLogging)
            {
                logger.LogWarning("FusionCache FailSafe Activated: KeyHash={KeyHash}, CacheName={CacheName}, " +
                    "Timestamp={Timestamp}", 
                    e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
            }
            else
            {
                logger.LogWarning("FusionCache FailSafe Activated: Key={Key}", e.Key);
            }
        };

        fusionCache.Events.FactoryError += (sender, e) =>
        {
            factoryErrorCounter.Add(1, 
                new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName),
                new KeyValuePair<string, object?>("error_type", "FactoryError"));
            
            if (enableDetailedLogging)
            {
                logger.LogError("FusionCache Factory Error: KeyHash={KeyHash}, CacheName={CacheName}, " +
                    "Timestamp={Timestamp}", 
                    e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
            }
            else
            {
                logger.LogError("FusionCache Factory Error: Key={Key}", e.Key);
            }
        };

        fusionCache.Events.BackgroundFactorySuccess += (sender, e) =>
        {
            backgroundSuccessCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            if (logger.IsEnabled(logLevel))
            {
                if (enableDetailedLogging)
                {
                    logger.Log(logLevel, "FusionCache Background Factory Success: KeyHash={KeyHash}, CacheName={CacheName}, " +
                        "Timestamp={Timestamp}", 
                        e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
                }
                else
                {
                    logger.Log(logLevel, "FusionCache Background Factory Success: Key={Key}", e.Key);
                }
            }
        };

        fusionCache.Events.BackgroundFactoryError += (sender, e) =>
        {
            backgroundErrorCounter.Add(1, 
                new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName),
                new KeyValuePair<string, object?>("error_type", "BackgroundFactoryError"));
            
            if (enableDetailedLogging)
            {
                logger.LogError("FusionCache Background Factory Error: KeyHash={KeyHash}, CacheName={CacheName}, " +
                    "Timestamp={Timestamp}", 
                    e.Key.GetHashCode(), fusionCache.CacheName, DateTimeOffset.UtcNow);
            }
            else
            {
                logger.LogError("FusionCache Background Factory Error: Key={Key}", e.Key);
            }
        };

        logger.LogInformation("FusionCache 이벤트 핸들러가 등록되었습니다. " +
            "CacheName: {CacheName}, LogLevel: {LogLevel}, DetailedLogging: {DetailedLogging}", 
            fusionCache.CacheName, logLevel, enableDetailedLogging);
    }

    /// <summary>
    /// FusionCache OpenTelemetry 계측을 설정합니다
    /// 캐시 작업에 대한 메트릭과 추적을 제공합니다
    /// Redis 인스트루멘테이션과 연동하여 분산 추적을 지원합니다
    /// </summary>
    /// <param name="fusionCache">FusionCache 인스턴스</param>
    /// <param name="logger">로거 인스턴스</param>
    private static void SetupOpenTelemetryInstrumentation(FusionCache fusionCache, ILogger logger)
    {
        // FusionCache OpenTelemetry 확장을 사용하여 자동 계측 활성화
        // 참고: FusionCache.OpenTelemetry 패키지는 자동으로 계측을 활성화합니다

        // 추가 메트릭 수집을 위한 Meter 생성
        var meter = new Meter("Demo.Infra.FusionCache", "1.0.0");
        
        // 캐시 히트율 추적을 위한 카운터
        var hitCounter = meter.CreateCounter<long>("fusion_cache_hits_total", 
            description: "FusionCache 히트 횟수");
        var missCounter = meter.CreateCounter<long>("fusion_cache_misses_total", 
            description: "FusionCache 미스 횟수");
        var setCounter = meter.CreateCounter<long>("fusion_cache_sets_total", 
            description: "FusionCache 설정 횟수");
        var failsafeCounter = meter.CreateCounter<long>("fusion_cache_failsafe_activations_total", 
            description: "FusionCache 페일세이프 활성화 횟수");
        var errorCounter = meter.CreateCounter<long>("fusion_cache_errors_total", 
            description: "FusionCache 오류 횟수");

        // 캐시 작업 지속 시간 추적을 위한 히스토그램
        var operationDuration = meter.CreateHistogram<double>("fusion_cache_operation_duration_seconds", 
            unit: "s", description: "FusionCache 작업 지속 시간");

        // 이벤트 핸들러를 통한 메트릭 수집
        fusionCache.Events.Hit += (sender, e) =>
        {
            hitCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            // 현재 Activity에 태그 추가 (분산 추적용)
            Activity.Current?.SetTag("fusion_cache.operation", "hit");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
        };

        fusionCache.Events.Miss += (sender, e) =>
        {
            missCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            Activity.Current?.SetTag("fusion_cache.operation", "miss");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
        };

        fusionCache.Events.Set += (sender, e) =>
        {
            setCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            Activity.Current?.SetTag("fusion_cache.operation", "set");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
        };

        fusionCache.Events.FailSafeActivate += (sender, e) =>
        {
            failsafeCounter.Add(1, new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName));
            
            Activity.Current?.SetTag("fusion_cache.operation", "failsafe_activate");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
        };

        fusionCache.Events.FactoryError += (sender, e) =>
        {
            errorCounter.Add(1, 
                new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName),
                new KeyValuePair<string, object?>("error_type", "factory_error"));
            
            Activity.Current?.SetTag("fusion_cache.operation", "factory_error");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
            Activity.Current?.SetTag("fusion_cache.error", true);
        };

        fusionCache.Events.BackgroundFactoryError += (sender, e) =>
        {
            errorCounter.Add(1, 
                new KeyValuePair<string, object?>("cache_name", fusionCache.CacheName),
                new KeyValuePair<string, object?>("error_type", "background_factory_error"));
            
            Activity.Current?.SetTag("fusion_cache.operation", "background_factory_error");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
            Activity.Current?.SetTag("fusion_cache.error", true);
        };

        // 백그라운드 새로고침 성공 추적
        fusionCache.Events.BackgroundFactorySuccess += (sender, e) =>
        {
            Activity.Current?.SetTag("fusion_cache.operation", "background_factory_success");
            Activity.Current?.SetTag("fusion_cache.cache_name", fusionCache.CacheName);
            Activity.Current?.SetTag("fusion_cache.key_hash", e.Key.GetHashCode().ToString());
        };

        logger.LogInformation("FusionCache OpenTelemetry 계측이 설정되었습니다. " +
            "메트릭 수집 및 분산 추적이 활성화되었습니다. CacheName: {CacheName}", fusionCache.CacheName);
    }

    /// <summary>
    /// 캐시 성능 메트릭을 수집하고 계산하는 백그라운드 서비스를 설정합니다
    /// 히트율, 미스율 등의 집계 메트릭을 제공합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">애플리케이션 구성</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddFusionCacheMetricsCollector(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FusionCacheConfig>(
            configuration.GetSection("FusionCache"));

        // 백그라운드 서비스로 메트릭 수집기 등록
        services.AddHostedService<FusionCacheMetricsCollectorService>();

        return services;
    }

    /// <summary>
    /// 전환 메커니즘을 지원하는 IpToNation 캐시 서비스를 등록합니다
    /// 기능 플래그와 트래픽 분할을 통한 점진적 마이그레이션을 지원합니다
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">애플리케이션 구성</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddIpToNationCacheWithMigration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 기존 Redis 캐시 서비스 등록
        services.AddIpToNationRedisCache(configuration);
        
        // FusionCache 서비스 등록
        services.AddIpToNationFusionCache(configuration);

        // 전환 메커니즘을 위한 래퍼 등록
        services.AddScoped<IpToNationCacheWrapper>();

        // IIpToNationCache 인터페이스에 래퍼 등록 (기존 등록을 덮어씀)
        services.AddScoped<IIpToNationCache>(serviceProvider =>
        {
            var config = serviceProvider.GetRequiredService<IOptionsMonitor<FusionCacheConfig>>().CurrentValue;
            
            // 전환 메커니즘이 활성화된 경우 래퍼 사용
            if (config.UseFusionCache || config.TrafficSplitRatio > 0.0)
            {
                return serviceProvider.GetRequiredService<IpToNationCacheWrapper>();
            }
            
            // 그렇지 않으면 기존 Redis 캐시 사용
            return serviceProvider.GetRequiredService<IpToNationRedisCache>();
        });

        return services;
    }

    /// <summary>
    /// 기존 Redis 캐시 서비스만 등록합니다 (호환성 유지)
    /// </summary>
    /// <param name="services">서비스 컬렉션</param>
    /// <param name="configuration">애플리케이션 구성</param>
    /// <returns>구성된 서비스 컬렉션</returns>
    public static IServiceCollection AddIpToNationRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 기존 RedisConfig 등록
        services.Configure<RedisConfig>(
            configuration.GetSection("Redis"));

        // Redis 연결 설정
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var redisConfig = serviceProvider.GetRequiredService<IOptions<RedisConfig>>().Value;
            var connectionString = redisConfig.IpToNationConnectionString;
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "Redis IpToNationConnectionString이 설정되지 않았습니다. appsettings.json에서 Redis:IpToNationConnectionString을 확인해주세요.");
            }

            return ConnectionMultiplexer.Connect(connectionString);
        });

        // 기존 Redis 캐시 구현체 등록
        services.AddScoped<IpToNationRedisCache>();
        services.AddScoped<IIpToNationCache, IpToNationRedisCache>();

        return services;
    }
}

/// <summary>
/// FusionCache 메트릭을 주기적으로 수집하고 집계하는 백그라운드 서비스
/// 캐시 히트율, 미스율, 평균 응답 시간 등의 메트릭을 계산합니다
/// </summary>
public class FusionCacheMetricsCollectorService : BackgroundService
{
    private readonly ILogger<FusionCacheMetricsCollectorService> _logger;
    private readonly FusionCacheConfig _config;
    private readonly Meter _meter;
    
    // 집계 메트릭을 위한 Instruments (실제로는 콜백에서 직접 생성됨)
    
    // 메트릭 수집을 위한 카운터
    private long _totalHits = 0;
    private long _totalMisses = 0;
    private long _totalSets = 0;
    private long _totalErrors = 0;
    private readonly object _metricsLock = new object();

    /// <summary>
    /// FusionCacheMetricsCollectorService의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="config">FusionCache 설정</param>
    public FusionCacheMetricsCollectorService(
        ILogger<FusionCacheMetricsCollectorService> logger,
        IOptions<FusionCacheConfig> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        
        _meter = new Meter("Demo.Infra.FusionCache.Metrics", "1.0.0");
        
        // 관찰 가능한 게이지는 아래 콜백에서 직접 생성됨

        // 메트릭 수집 콜백 등록
        _meter.CreateObservableGauge("fusion_cache_hit_rate_current", () =>
        {
            lock (_metricsLock)
            {
                var total = _totalHits + _totalMisses;
                return total > 0 ? (_totalHits * 100.0 / total) : 0.0;
            }
        }, "%", "현재 FusionCache 히트율");

        _meter.CreateObservableGauge("fusion_cache_miss_rate_current", () =>
        {
            lock (_metricsLock)
            {
                var total = _totalHits + _totalMisses;
                return total > 0 ? (_totalMisses * 100.0 / total) : 0.0;
            }
        }, "%", "현재 FusionCache 미스율");

        _meter.CreateObservableGauge("fusion_cache_total_hits_current", () =>
        {
            lock (_metricsLock)
            {
                return _totalHits;
            }
        }, description: "현재 FusionCache 총 히트 수");

        _meter.CreateObservableGauge("fusion_cache_total_misses_current", () =>
        {
            lock (_metricsLock)
            {
                return _totalMisses;
            }
        }, description: "현재 FusionCache 총 미스 수");

        _meter.CreateObservableGauge("fusion_cache_total_sets_current", () =>
        {
            lock (_metricsLock)
            {
                return _totalSets;
            }
        }, description: "현재 FusionCache 총 설정 수");

        _meter.CreateObservableGauge("fusion_cache_total_errors_current", () =>
        {
            lock (_metricsLock)
            {
                return _totalErrors;
            }
        }, description: "현재 FusionCache 총 오류 수");

        _logger.LogInformation("FusionCache 메트릭 수집기가 초기화되었습니다. " +
            "수집 간격: {IntervalSeconds}초", _config.MetricsCollectionIntervalSeconds);
    }

    /// <summary>
    /// 백그라운드 메트릭 수집 작업을 실행합니다
    /// </summary>
    /// <param name="stoppingToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_config.MetricsCollectionIntervalSeconds <= 0)
        {
            _logger.LogInformation("메트릭 수집이 비활성화되었습니다 (간격: {Interval}초)", 
                _config.MetricsCollectionIntervalSeconds);
            return;
        }

        var interval = TimeSpan.FromSeconds(_config.MetricsCollectionIntervalSeconds);
        
        _logger.LogInformation("FusionCache 메트릭 수집을 시작합니다. 간격: {Interval}", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectMetricsAsync();
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 정상적인 종료
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "메트릭 수집 중 오류가 발생했습니다");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // 오류 시 30초 대기
            }
        }

        _logger.LogInformation("FusionCache 메트릭 수집이 중지되었습니다");
    }

    /// <summary>
    /// 메트릭을 수집하고 로그에 기록합니다
    /// </summary>
    /// <returns>비동기 작업</returns>
    private async Task CollectMetricsAsync()
    {
        lock (_metricsLock)
        {
            var total = _totalHits + _totalMisses;
            var hitRate = total > 0 ? (_totalHits * 100.0 / total) : 0.0;
            var missRate = total > 0 ? (_totalMisses * 100.0 / total) : 0.0;

            if (_config.EnableDetailedLogging)
            {
                _logger.LogInformation("FusionCache 메트릭 수집 완료: " +
                    "총 작업: {TotalOperations}, 히트: {Hits}, 미스: {Misses}, 설정: {Sets}, 오류: {Errors}, " +
                    "히트율: {HitRate:F2}%, 미스율: {MissRate:F2}%",
                    total, _totalHits, _totalMisses, _totalSets, _totalErrors, hitRate, missRate);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 메트릭 카운터를 업데이트합니다
    /// </summary>
    /// <param name="operation">작업 유형</param>
    /// <param name="count">증가할 수</param>
    public void UpdateMetrics(string operation, long count = 1)
    {
        lock (_metricsLock)
        {
            switch (operation.ToLowerInvariant())
            {
                case "hit":
                    _totalHits += count;
                    break;
                case "miss":
                    _totalMisses += count;
                    break;
                case "set":
                    _totalSets += count;
                    break;
                case "error":
                    _totalErrors += count;
                    break;
            }
        }
    }

    /// <summary>
    /// 리소스를 정리합니다
    /// </summary>
    public override void Dispose()
    {
        _meter?.Dispose();
        base.Dispose();
    }
}