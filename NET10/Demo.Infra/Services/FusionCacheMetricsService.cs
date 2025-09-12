using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Demo.Infra.Configs;
using System.Diagnostics.Metrics;
using System.Collections.Concurrent;

namespace Demo.Infra.Services;

/// <summary>
/// FusionCache 메트릭 수집 및 계산을 담당하는 서비스
/// 캐시 히트율, 미스율, 성능 지표 등을 실시간으로 추적합니다
/// 요구사항 4.2, 4.3 구현
/// </summary>
public class FusionCacheMetricsService : IDisposable
{
    private readonly ILogger<FusionCacheMetricsService> _logger;
    private readonly FusionCacheConfig _config;
    private readonly Meter _meter;
    
    // 메트릭 수집을 위한 스레드 안전한 카운터들
    private readonly ConcurrentDictionary<string, MetricCounters> _cacheMetrics = new();
    
    // 관찰 가능한 메트릭들
    private readonly ObservableGauge<double> _hitRateGauge;
    private readonly ObservableGauge<double> _missRateGauge;
    private readonly ObservableGauge<long> _totalOperationsGauge;
    private readonly ObservableGauge<long> _totalHitsGauge;
    private readonly ObservableGauge<long> _totalMissesGauge;
    private readonly ObservableGauge<long> _totalSetsGauge;
    private readonly ObservableGauge<long> _totalErrorsGauge;
    private readonly ObservableGauge<double> _averageResponseTimeGauge;

    /// <summary>
    /// FusionCacheMetricsService의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="config">FusionCache 설정</param>
    public FusionCacheMetricsService(
        ILogger<FusionCacheMetricsService> logger,
        IOptions<FusionCacheConfig> config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        
        _meter = new Meter("Demo.Infra.FusionCache.Metrics", "1.0.0");
        
        // 관찰 가능한 게이지 메트릭 생성
        _hitRateGauge = _meter.CreateObservableGauge("fusion_cache_hit_rate_percent", 
            () => GetAggregatedHitRate(), 
            "%", "FusionCache 전체 히트율");

        _missRateGauge = _meter.CreateObservableGauge("fusion_cache_miss_rate_percent", 
            () => GetAggregatedMissRate(), 
            "%", "FusionCache 전체 미스율");

        _totalOperationsGauge = _meter.CreateObservableGauge("fusion_cache_total_operations", 
            () => GetAggregatedTotalOperations(), 
            description: "FusionCache 전체 작업 수");

        _totalHitsGauge = _meter.CreateObservableGauge("fusion_cache_total_hits", 
            () => GetAggregatedTotalHits(), 
            description: "FusionCache 전체 히트 수");

        _totalMissesGauge = _meter.CreateObservableGauge("fusion_cache_total_misses", 
            () => GetAggregatedTotalMisses(), 
            description: "FusionCache 전체 미스 수");

        _totalSetsGauge = _meter.CreateObservableGauge("fusion_cache_total_sets", 
            () => GetAggregatedTotalSets(), 
            description: "FusionCache 전체 설정 수");

        _totalErrorsGauge = _meter.CreateObservableGauge("fusion_cache_total_errors", 
            () => GetAggregatedTotalErrors(), 
            description: "FusionCache 전체 오류 수");

        _averageResponseTimeGauge = _meter.CreateObservableGauge("fusion_cache_average_response_time_ms", 
            () => GetAggregatedAverageResponseTime(), 
            "ms", "FusionCache 평균 응답 시간");

        _logger.LogInformation("FusionCache 메트릭 서비스가 초기화되었습니다. " +
            "메트릭 수집이 활성화되었습니다");
    }

    /// <summary>
    /// 캐시 작업 메트릭을 기록합니다
    /// </summary>
    /// <param name="cacheName">캐시 이름</param>
    /// <param name="operation">작업 유형 (get, set, remove 등)</param>
    /// <param name="result">작업 결과 (hit, miss, success, error 등)</param>
    /// <param name="durationMs">작업 지속 시간 (밀리초)</param>
    /// <param name="additionalTags">추가 태그</param>
    public void RecordCacheOperation(
        string cacheName, 
        string operation, 
        string result, 
        double durationMs,
        Dictionary<string, object?>? additionalTags = null)
    {
        if (!_config.EnableMetrics)
        {
            return;
        }

        // CacheName이 null이거나 빈 문자열인 경우 기본값 사용
        var safeCacheName = string.IsNullOrEmpty(cacheName) ? "UnknownCache" : cacheName;
        var metrics = _cacheMetrics.GetOrAdd(safeCacheName, _ => new MetricCounters());
        
        // 작업 유형별 카운터 업데이트
        switch (operation.ToLowerInvariant())
        {
            case "get":
                if (result.Equals("hit", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.IncrementHits();
                }
                else if (result.Equals("miss", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.IncrementMisses();
                }
                else if (result.Equals("error", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.IncrementErrors();
                }
                break;
                
            case "set":
                if (result.Equals("success", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.IncrementSets();
                }
                else if (result.Equals("error", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.IncrementErrors();
                }
                break;
                
            default:
                if (result.Equals("error", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.IncrementErrors();
                }
                break;
        }

        // 응답 시간 업데이트
        metrics.UpdateResponseTime(durationMs);

        // 상세 로깅이 활성화된 경우 메트릭 로깅
        if (_config.EnableDetailedLogging)
        {
            var tags = additionalTags != null ? 
                string.Join(", ", additionalTags.Select(kvp => $"{kvp.Key}={kvp.Value}")) : 
                "none";
                
            _logger.LogDebug("캐시 메트릭 기록: CacheName={CacheName}, Operation={Operation}, " +
                "Result={Result}, Duration={Duration}ms, Tags={Tags}", 
                safeCacheName, operation, result, durationMs, tags);
        }
    }

    /// <summary>
    /// 특정 캐시의 현재 메트릭을 가져옵니다
    /// </summary>
    /// <param name="cacheName">캐시 이름</param>
    /// <returns>캐시 메트릭 정보</returns>
    public CacheMetricsSnapshot? GetCacheMetrics(string cacheName)
    {
        if (!_cacheMetrics.TryGetValue(cacheName, out var metrics))
        {
            return null;
        }

        return metrics.GetSnapshot();
    }

    /// <summary>
    /// 모든 캐시의 집계된 메트릭을 가져옵니다
    /// </summary>
    /// <returns>집계된 캐시 메트릭 정보</returns>
    public CacheMetricsSnapshot GetAggregatedMetrics()
    {
        var totalHits = 0L;
        var totalMisses = 0L;
        var totalSets = 0L;
        var totalErrors = 0L;
        var totalResponseTime = 0.0;
        var totalOperations = 0L;

        foreach (var metrics in _cacheMetrics.Values)
        {
            var snapshot = metrics.GetSnapshot();
            totalHits += snapshot.TotalHits;
            totalMisses += snapshot.TotalMisses;
            totalSets += snapshot.TotalSets;
            totalErrors += snapshot.TotalErrors;
            totalResponseTime += snapshot.AverageResponseTimeMs * (snapshot.TotalHits + snapshot.TotalMisses);
            totalOperations += snapshot.TotalHits + snapshot.TotalMisses;
        }

        var averageResponseTime = totalOperations > 0 ? totalResponseTime / totalOperations : 0.0;
        var total = totalHits + totalMisses;
        var hitRate = total > 0 ? (totalHits * 100.0 / total) : 0.0;
        var missRate = total > 0 ? (totalMisses * 100.0 / total) : 0.0;

        return new CacheMetricsSnapshot
        {
            TotalHits = totalHits,
            TotalMisses = totalMisses,
            TotalSets = totalSets,
            TotalErrors = totalErrors,
            HitRatePercent = hitRate,
            MissRatePercent = missRate,
            AverageResponseTimeMs = averageResponseTime,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// 메트릭을 초기화합니다
    /// </summary>
    /// <param name="cacheName">초기화할 캐시 이름 (null이면 모든 캐시)</param>
    public void ResetMetrics(string? cacheName = null)
    {
        if (cacheName != null)
        {
            if (_cacheMetrics.TryGetValue(cacheName, out var metrics))
            {
                metrics.Reset();
                _logger.LogInformation("캐시 메트릭이 초기화되었습니다: {CacheName}", cacheName);
            }
        }
        else
        {
            foreach (var metrics in _cacheMetrics.Values)
            {
                metrics.Reset();
            }
            _logger.LogInformation("모든 캐시 메트릭이 초기화되었습니다");
        }
    }

    /// <summary>
    /// 메트릭 요약 정보를 로그에 기록합니다
    /// </summary>
    public void LogMetricsSummary()
    {
        if (!_config.EnableMetrics)
        {
            return;
        }

        var aggregated = GetAggregatedMetrics();
        
        _logger.LogInformation("FusionCache 메트릭 요약: " +
            "총 작업: {TotalOperations}, 히트: {TotalHits}, 미스: {TotalMisses}, " +
            "설정: {TotalSets}, 오류: {TotalErrors}, " +
            "히트율: {HitRate:F2}%, 미스율: {MissRate:F2}%, " +
            "평균 응답시간: {AverageResponseTime:F2}ms",
            aggregated.TotalHits + aggregated.TotalMisses,
            aggregated.TotalHits,
            aggregated.TotalMisses,
            aggregated.TotalSets,
            aggregated.TotalErrors,
            aggregated.HitRatePercent,
            aggregated.MissRatePercent,
            aggregated.AverageResponseTimeMs);

        // 캐시별 상세 메트릭 로깅 (상세 로깅이 활성화된 경우)
        if (_config.EnableDetailedLogging)
        {
            foreach (var kvp in _cacheMetrics)
            {
                var snapshot = kvp.Value.GetSnapshot();
                var total = snapshot.TotalHits + snapshot.TotalMisses;
                
                if (total > 0)
                {
                    _logger.LogInformation("캐시별 메트릭 [{CacheName}]: " +
                        "작업: {TotalOperations}, 히트: {TotalHits}, 미스: {TotalMisses}, " +
                        "설정: {TotalSets}, 오류: {TotalErrors}, " +
                        "히트율: {HitRate:F2}%, 평균 응답시간: {AverageResponseTime:F2}ms",
                        kvp.Key,
                        total,
                        snapshot.TotalHits,
                        snapshot.TotalMisses,
                        snapshot.TotalSets,
                        snapshot.TotalErrors,
                        snapshot.HitRatePercent,
                        snapshot.AverageResponseTimeMs);
                }
            }
        }
    }

    // 관찰 가능한 메트릭을 위한 집계 메서드들
    private double GetAggregatedHitRate() => GetAggregatedMetrics().HitRatePercent;
    private double GetAggregatedMissRate() => GetAggregatedMetrics().MissRatePercent;
    private long GetAggregatedTotalOperations() => GetAggregatedMetrics().TotalHits + GetAggregatedMetrics().TotalMisses;
    private long GetAggregatedTotalHits() => GetAggregatedMetrics().TotalHits;
    private long GetAggregatedTotalMisses() => GetAggregatedMetrics().TotalMisses;
    private long GetAggregatedTotalSets() => GetAggregatedMetrics().TotalSets;
    private long GetAggregatedTotalErrors() => GetAggregatedMetrics().TotalErrors;
    private double GetAggregatedAverageResponseTime() => GetAggregatedMetrics().AverageResponseTimeMs;

    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// 스레드 안전한 메트릭 카운터 클래스
/// </summary>
internal class MetricCounters
{
    private long _hits = 0;
    private long _misses = 0;
    private long _sets = 0;
    private long _errors = 0;
    private long _totalResponseTimeMs = 0;
    private long _responseTimeCount = 0;
    private readonly object _lock = new();

    public void IncrementHits() => Interlocked.Increment(ref _hits);
    public void IncrementMisses() => Interlocked.Increment(ref _misses);
    public void IncrementSets() => Interlocked.Increment(ref _sets);
    public void IncrementErrors() => Interlocked.Increment(ref _errors);

    public void UpdateResponseTime(double durationMs)
    {
        // 정밀도를 유지하기 위해 100배로 스케일링하여 저장
        var scaledDurationMs = (long)Math.Round(durationMs * 100);
        Interlocked.Add(ref _totalResponseTimeMs, scaledDurationMs);
        Interlocked.Increment(ref _responseTimeCount);
    }

    public CacheMetricsSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            var hits = Interlocked.Read(ref _hits);
            var misses = Interlocked.Read(ref _misses);
            var sets = Interlocked.Read(ref _sets);
            var errors = Interlocked.Read(ref _errors);
            var totalResponseTime = Interlocked.Read(ref _totalResponseTimeMs);
            var responseTimeCount = Interlocked.Read(ref _responseTimeCount);

            var total = hits + misses;
            var hitRate = total > 0 ? (hits * 100.0 / total) : 0.0;
            var missRate = total > 0 ? (misses * 100.0 / total) : 0.0;
            var averageResponseTime = responseTimeCount > 0 ? (totalResponseTime / (double)responseTimeCount / 100.0) : 0.0;

            return new CacheMetricsSnapshot
            {
                TotalHits = hits,
                TotalMisses = misses,
                TotalSets = sets,
                TotalErrors = errors,
                HitRatePercent = hitRate,
                MissRatePercent = missRate,
                AverageResponseTimeMs = averageResponseTime,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
            Interlocked.Exchange(ref _sets, 0);
            Interlocked.Exchange(ref _errors, 0);
            Interlocked.Exchange(ref _totalResponseTimeMs, 0);
            Interlocked.Exchange(ref _responseTimeCount, 0);
        }
    }
}

/// <summary>
/// 캐시 메트릭 스냅샷 정보
/// </summary>
public class CacheMetricsSnapshot
{
    /// <summary>
    /// 총 히트 수
    /// </summary>
    public long TotalHits { get; set; }

    /// <summary>
    /// 총 미스 수
    /// </summary>
    public long TotalMisses { get; set; }

    /// <summary>
    /// 총 설정 수
    /// </summary>
    public long TotalSets { get; set; }

    /// <summary>
    /// 총 오류 수
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// 히트율 (퍼센트)
    /// </summary>
    public double HitRatePercent { get; set; }

    /// <summary>
    /// 미스율 (퍼센트)
    /// </summary>
    public double MissRatePercent { get; set; }

    /// <summary>
    /// 평균 응답 시간 (밀리초)
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    /// <summary>
    /// 스냅샷 생성 시간
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}