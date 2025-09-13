using Demo.Domain.Repositories;
using Demo.Infra.Configs;
using Demo.Infra.Services;
using FluentResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Demo.Infra.Repositories;

/// <summary>
/// FusionCache를 사용한 IP 주소에서 국가 코드 매핑 캐시 구현체
/// L1(메모리) + L2(Redis) 하이브리드 캐시 구조를 제공합니다
/// 기존 RedisConfig와 호환성을 유지하면서 FusionCache의 고급 기능을 제공합니다
/// 구조화된 로깅과 메트릭 수집 기능을 포함합니다
/// </summary>
public class IpToNationFusionCache : IIpToNationCache
{
    private readonly IFusionCache _fusionCache;
    private readonly string? _keyPrefix;
    private readonly ILogger<IpToNationFusionCache> _logger;
    private readonly bool _enableDetailedLogging;
    private readonly FusionCacheMetricsService? _metricsService;
    
    // 메트릭 수집을 위한 Meter 및 Instruments
    private static readonly Meter _meter = new("Demo.Infra.IpToNationFusionCache", "1.0.0");
    private static readonly Counter<long> _cacheHitCounter = _meter.CreateCounter<long>(
        "ip_nation_cache_hits_total", 
        description: "IP 국가 코드 캐시 히트 횟수");
    private static readonly Counter<long> _cacheMissCounter = _meter.CreateCounter<long>(
        "ip_nation_cache_misses_total", 
        description: "IP 국가 코드 캐시 미스 횟수");
    private static readonly Counter<long> _cacheSetCounter = _meter.CreateCounter<long>(
        "ip_nation_cache_sets_total", 
        description: "IP 국가 코드 캐시 설정 횟수");
    private static readonly Counter<long> _cacheErrorCounter = _meter.CreateCounter<long>(
        "ip_nation_cache_errors_total", 
        description: "IP 국가 코드 캐시 오류 횟수");
    private static readonly Histogram<double> _cacheOperationDuration = _meter.CreateHistogram<double>(
        "ip_nation_cache_operation_duration_seconds", 
        unit: "s", 
        description: "IP 국가 코드 캐시 작업 지속 시간");

    /// <summary>
    /// IpToNationFusionCache 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="fusionCache">FusionCache 인스턴스</param>
    /// <param name="fusionCacheConfig">FusionCache 설정 옵션 (RedisConfig 통합)</param>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="metricsService">메트릭 서비스 (선택적)</param>
    public IpToNationFusionCache(
        IFusionCache fusionCache,
        IOptions<FusionCacheConfig> fusionCacheConfig,
        ILogger<IpToNationFusionCache> logger,
        FusionCacheMetricsService? metricsService = null)
    {
        _fusionCache = fusionCache ?? throw new ArgumentNullException(nameof(fusionCache));
        _keyPrefix = fusionCacheConfig?.Value?.KeyPrefix;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enableDetailedLogging = fusionCacheConfig?.Value?.EnableDetailedLogging ?? false;
        _metricsService = metricsService;
        
        if (_enableDetailedLogging)
        {
            _logger.LogInformation("IpToNationFusionCache 초기화됨. " +
                "KeyPrefix: {KeyPrefix}, DetailedLogging: {DetailedLogging}, " +
                "CacheName: {CacheName}, MetricsEnabled: {MetricsEnabled}", 
                _keyPrefix, _enableDetailedLogging, _fusionCache.CacheName, _metricsService != null);
        }
        else
        {
            _logger.LogDebug("IpToNationFusionCache 초기화됨. KeyPrefix: {KeyPrefix}", _keyPrefix);
        }
    }

    /// <summary>
    /// 주어진 클라이언트 IP에 대한 캐시 키를 생성합니다
    /// 기존 IpToNationRedisCache와 동일한 키 형식을 유지합니다
    /// </summary>
    /// <param name="clientIp">클라이언트 IP 주소</param>
    /// <returns>포맷된 캐시 키</returns>
    private string MakeKey(string clientIp)
    {
        return string.IsNullOrEmpty(_keyPrefix) ?
            $"ipcache:{clientIp}" :
            $"{_keyPrefix}:ipcache:{clientIp}";
    }

    /// <summary>
    /// 지정된 클라이언트 IP 주소에 대한 캐시된 국가 코드를 비동기적으로 검색합니다
    /// 구조화된 로깅과 메트릭 수집을 포함합니다
    /// </summary>
    /// <param name="clientIp">캐시에서 조회할 IP 주소</param>
    /// <returns>국가 코드가 발견되면 해당 코드를 포함한 Result; 그렇지 않으면 "Not found" 메시지와 함께 실패 Result</returns>
    public async Task<Result<string>> GetAsync(string clientIp)
    {
        var stopwatch = Stopwatch.StartNew();
        var key = MakeKey(clientIp);
        
        // 분산 추적을 위한 Activity 태그 설정
        using var activity = Activity.Current?.Source.StartActivity("IpToNationCache.GetAsync");
        activity?.SetTag("cache.operation", "get");
        activity?.SetTag("cache.key_hash", key.GetHashCode().ToString());
        activity?.SetTag("cache.client_ip_hash", clientIp.GetHashCode().ToString());
        activity?.SetTag("cache.implementation", "FusionCache");

        try
        {
            var result = await _fusionCache.GetOrDefaultAsync<string>(key);
            stopwatch.Stop();
            
            if (result is null)
            {
                // 캐시 미스 메트릭 및 로깅
                _cacheMissCounter.Add(1, 
                    new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName),
                    new KeyValuePair<string, object?>("key_prefix", _keyPrefix ?? "none"));
                
                _cacheOperationDuration.Record(stopwatch.Elapsed.TotalSeconds,
                    new KeyValuePair<string, object?>("operation", "get"),
                    new KeyValuePair<string, object?>("result", "miss"),
                    new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));

                // 개선된 메트릭 서비스 사용
                _metricsService?.RecordCacheOperation(
                    _fusionCache.CacheName, 
                    "get", 
                    "miss", 
                    stopwatch.Elapsed.TotalMilliseconds,
                    new Dictionary<string, object?>
                    {
                        ["key_prefix"] = _keyPrefix ?? "none",
                        ["client_ip_hash"] = clientIp.GetHashCode()
                    });

                activity?.SetTag("cache.result", "miss");
                activity?.SetStatus(ActivityStatusCode.Ok, "Cache miss");

                if (_enableDetailedLogging)
                {
                    _logger.LogInformation("캐시 미스: IP {ClientIpHash}에 대한 국가 코드를 찾을 수 없습니다. " +
                        "Duration: {Duration}ms, Key: {KeyHash}", 
                        clientIp.GetHashCode(), stopwatch.ElapsedMilliseconds, key.GetHashCode());
                }
                else
                {
                    _logger.LogDebug("캐시 미스: IP {ClientIp}에 대한 국가 코드를 찾을 수 없습니다", clientIp);
                }
                
                return Result.Fail("Not found");
            }

            // 캐시 히트 메트릭 및 로깅
            _cacheHitCounter.Add(1, 
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName),
                new KeyValuePair<string, object?>("key_prefix", _keyPrefix ?? "none"));
            
            _cacheOperationDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "get"),
                new KeyValuePair<string, object?>("result", "hit"),
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));

            // 개선된 메트릭 서비스 사용
            _metricsService?.RecordCacheOperation(
                _fusionCache.CacheName, 
                "get", 
                "hit", 
                stopwatch.Elapsed.TotalMilliseconds,
                new Dictionary<string, object?>
                {
                    ["key_prefix"] = _keyPrefix ?? "none",
                    ["client_ip_hash"] = clientIp.GetHashCode(),
                    ["country_code"] = result
                });

            activity?.SetTag("cache.result", "hit");
            activity?.SetTag("cache.country_code", result);
            activity?.SetStatus(ActivityStatusCode.Ok, "Cache hit");

            if (_enableDetailedLogging)
            {
                _logger.LogInformation("캐시 히트: IP {ClientIpHash}에 대한 국가 코드 {CountryCode}를 반환합니다. " +
                    "Duration: {Duration}ms, Key: {KeyHash}", 
                    clientIp.GetHashCode(), result, stopwatch.ElapsedMilliseconds, key.GetHashCode());
            }
            else
            {
                _logger.LogDebug("캐시 히트: IP {ClientIp}에 대한 국가 코드 {CountryCode}를 반환합니다", clientIp, result);
            }
            
            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 오류 메트릭 및 로깅
            _cacheErrorCounter.Add(1, 
                new KeyValuePair<string, object?>("operation", "get"),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name),
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));
            
            _cacheOperationDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "get"),
                new KeyValuePair<string, object?>("result", "error"),
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));

            // 개선된 메트릭 서비스 사용
            _metricsService?.RecordCacheOperation(
                _fusionCache.CacheName, 
                "get", 
                "error", 
                stopwatch.Elapsed.TotalMilliseconds,
                new Dictionary<string, object?>
                {
                    ["key_prefix"] = _keyPrefix ?? "none",
                    ["client_ip_hash"] = clientIp.GetHashCode(),
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message
                });

            activity?.SetTag("cache.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            if (_enableDetailedLogging)
            {
                _logger.LogError(ex, "IP {ClientIpHash}에 대한 캐시 조회 중 오류가 발생했습니다. " +
                    "Duration: {Duration}ms, Key: {KeyHash}, ErrorType: {ErrorType}", 
                    clientIp.GetHashCode(), stopwatch.ElapsedMilliseconds, key.GetHashCode(), ex.GetType().Name);
            }
            else
            {
                _logger.LogError(ex, "IP {ClientIp}에 대한 캐시 조회 중 오류가 발생했습니다", clientIp);
            }
            
            return Result.Fail($"캐시 조회 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// 지정된 IP 주소에 대한 국가 코드를 캐시에 설정합니다
    /// 구조화된 로깅과 메트릭 수집을 포함합니다
    /// </summary>
    /// <param name="clientIp">클라이언트 IP 주소</param>
    /// <param name="countryCode">캐시할 국가 코드</param>
    /// <param name="ts">캐시 만료 시간</param>
    public async Task SetAsync(string clientIp, string countryCode, TimeSpan ts)
    {
        var stopwatch = Stopwatch.StartNew();
        var key = MakeKey(clientIp);
        
        // 분산 추적을 위한 Activity 태그 설정
        using var activity = Activity.Current?.Source.StartActivity("IpToNationCache.SetAsync");
        activity?.SetTag("cache.operation", "set");
        activity?.SetTag("cache.key_hash", key.GetHashCode().ToString());
        activity?.SetTag("cache.client_ip_hash", clientIp.GetHashCode().ToString());
        activity?.SetTag("cache.country_code", countryCode);
        activity?.SetTag("cache.duration_seconds", ts.TotalSeconds.ToString());
        activity?.SetTag("cache.implementation", "FusionCache");

        try
        {
            // TimeSpan을 FusionCacheEntryOptions로 변환
            var entryOptions = new FusionCacheEntryOptions
            {
                Duration = ts,
                Priority = CacheItemPriority.Normal,
                Size = 1,
                FailSafeMaxDuration = TimeSpan.FromHours(1),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30)
            };

            await _fusionCache.SetAsync(key, countryCode, entryOptions);
            stopwatch.Stop();
            
            // 캐시 설정 메트릭 및 로깅
            _cacheSetCounter.Add(1, 
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName),
                new KeyValuePair<string, object?>("key_prefix", _keyPrefix ?? "none"),
                new KeyValuePair<string, object?>("country_code", countryCode));
            
            _cacheOperationDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "set"),
                new KeyValuePair<string, object?>("result", "success"),
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));

            // 개선된 메트릭 서비스 사용
            _metricsService?.RecordCacheOperation(
                _fusionCache.CacheName, 
                "set", 
                "success", 
                stopwatch.Elapsed.TotalMilliseconds,
                new Dictionary<string, object?>
                {
                    ["key_prefix"] = _keyPrefix ?? "none",
                    ["client_ip_hash"] = clientIp.GetHashCode(),
                    ["country_code"] = countryCode,
                    ["cache_duration_seconds"] = ts.TotalSeconds
                });

            activity?.SetTag("cache.result", "success");
            activity?.SetStatus(ActivityStatusCode.Ok, "Cache set successful");

            if (_enableDetailedLogging)
            {
                _logger.LogInformation("캐시 설정 완료: IP {ClientIpHash}에 대한 국가 코드 {CountryCode}를 {Duration}동안 저장했습니다. " +
                    "Duration: {OperationDuration}ms, Key: {KeyHash}", 
                    clientIp.GetHashCode(), countryCode, ts, stopwatch.ElapsedMilliseconds, key.GetHashCode());
            }
            else
            {
                _logger.LogDebug("캐시 설정 완료: IP {ClientIp}에 대한 국가 코드 {CountryCode}를 {Duration}동안 저장했습니다", 
                    clientIp, countryCode, ts);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 오류 메트릭 및 로깅
            _cacheErrorCounter.Add(1, 
                new KeyValuePair<string, object?>("operation", "set"),
                new KeyValuePair<string, object?>("error_type", ex.GetType().Name),
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));
            
            _cacheOperationDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "set"),
                new KeyValuePair<string, object?>("result", "error"),
                new KeyValuePair<string, object?>("cache_name", _fusionCache.CacheName));

            // 개선된 메트릭 서비스 사용
            _metricsService?.RecordCacheOperation(
                _fusionCache.CacheName, 
                "set", 
                "error", 
                stopwatch.Elapsed.TotalMilliseconds,
                new Dictionary<string, object?>
                {
                    ["key_prefix"] = _keyPrefix ?? "none",
                    ["client_ip_hash"] = clientIp.GetHashCode(),
                    ["country_code"] = countryCode,
                    ["cache_duration_seconds"] = ts.TotalSeconds,
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message
                });

            activity?.SetTag("cache.result", "error");
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            if (_enableDetailedLogging)
            {
                _logger.LogError(ex, "IP {ClientIpHash}에 대한 캐시 설정 중 오류가 발생했습니다. " +
                    "Duration: {Duration}ms, Key: {KeyHash}, CountryCode: {CountryCode}, ErrorType: {ErrorType}", 
                    clientIp.GetHashCode(), stopwatch.ElapsedMilliseconds, key.GetHashCode(), countryCode, ex.GetType().Name);
            }
            else
            {
                _logger.LogError(ex, "IP {ClientIp}에 대한 캐시 설정 중 오류가 발생했습니다", clientIp);
            }
            
            throw;
        }
    }
}