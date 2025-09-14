using Demo.Application.Configs;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Demo.Infra.Configs;

/// <summary>
/// FusionCache 설정을 위한 구성 클래스
/// L1(메모리) + L2(Redis) 하이브리드 캐시 옵션을 정의합니다
/// 기존 RedisConfig와 통합하여 Redis 연결 설정을 재사용합니다
/// </summary>
public class FusionCacheConfig
{
    /// <summary>
    /// 설정 섹션 이름
    /// </summary>
    public const string SectionName = "FusionCache";

    /// <summary>
    /// Redis 설정 (기존 RedisConfig와 호환성 유지)
    /// 런타임에 주입되며, appsettings.json의 Redis 섹션에서 가져옵니다
    /// </summary>
    public RedisConfig? Redis { get; set; }

    /// <summary>
    /// Redis 연결 문자열 (Redis.IpToNationConnectionString에서 가져옴)
    /// </summary>
    public string ConnectionString => Redis?.IpToNationConnectionString ?? string.Empty;

    /// <summary>
    /// Redis 키 접두사 (Redis.KeyPrefix에서 가져옴)
    /// </summary>
    public string KeyPrefix => Redis?.KeyPrefix ?? string.Empty;
    /// <summary>
    /// 기본 캐시 항목 지속 시간
    /// 최소 1분, 최대 24시간
    /// </summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00", 
        ErrorMessage = "DefaultEntryOptions는 1분 이상 24시간 이하여야 합니다.")]
    public TimeSpan DefaultEntryOptions { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// L1 메모리 캐시 지속 시간
    /// 최소 30초, 최대 1시간
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:30", "01:00:00", 
        ErrorMessage = "L1CacheDuration은 30초 이상 1시간 이하여야 합니다.")]
    public TimeSpan L1CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 소프트 타임아웃 (백그라운드에서 계속 시도)
    /// 최소 100ms, 최대 10초
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00.100", "00:00:10", 
        ErrorMessage = "SoftTimeout은 100ms 이상 10초 이하여야 합니다.")]
    public TimeSpan SoftTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 하드 타임아웃 (완전 중단)
    /// 최소 1초, 최대 30초, SoftTimeout보다 커야 함
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:00:30", 
        ErrorMessage = "HardTimeout은 1초 이상 30초 이하여야 합니다.")]
    public TimeSpan HardTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// 페일세이프 메커니즘 활성화 여부
    /// Redis 장애 시 만료된 캐시 데이터라도 반환
    /// </summary>
    public bool EnableFailSafe { get; set; } = true;

    /// <summary>
    /// 백그라운드 새로고침 활성화 여부
    /// 만료 전 자동 갱신 기능
    /// </summary>
    public bool EnableEagerRefresh { get; set; } = true;

    /// <summary>
    /// 페일세이프 최대 지속 시간
    /// 장애 시 만료된 데이터를 얼마나 오래 사용할지 설정
    /// 최소 10분, 최대 24시간
    /// </summary>
    [Range(typeof(TimeSpan), "00:10:00", "1.00:00:00", 
        ErrorMessage = "FailSafeMaxDuration은 10분 이상 24시간 이하여야 합니다.")]
    public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 페일세이프 스로틀 지속 시간
    /// 연속적인 실패 시 재시도 간격
    /// 최소 5초, 최대 5분
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:05", "00:05:00", 
        ErrorMessage = "FailSafeThrottleDuration은 5초 이상 5분 이하여야 합니다.")]
    public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 백그라운드 새로고침 임계점
    /// 만료 시간의 몇 퍼센트에서 새로고침을 시작할지 설정 (0.1 ~ 0.95)
    /// </summary>
    [Range(0.1f, 0.95f, ErrorMessage = "EagerRefreshThreshold는 0.1 이상 0.95 이하여야 합니다.")]
    public float EagerRefreshThreshold { get; set; } = 0.8f;

    /// <summary>
    /// L1 캐시 최대 항목 수
    /// 메모리 사용량 제한 (최소 100, 최대 10000)
    /// </summary>
    [Range(100, 10000, ErrorMessage = "L1CacheMaxSize는 100 이상 10000 이하여야 합니다.")]
    public int L1CacheMaxSize { get; set; } = 1000;

    /// <summary>
    /// 캐시 스탬피드 방지 활성화 여부
    /// 동일 키에 대한 동시 요청 시 하나만 실행
    /// </summary>
    public bool EnableCacheStampedeProtection { get; set; } = true;

    /// <summary>
    /// OpenTelemetry 계측 활성화 여부
    /// </summary>
    public bool EnableOpenTelemetry { get; set; } = true;

    /// <summary>
    /// 상세 로깅 활성화 여부
    /// 활성화 시 IP 주소는 해시값으로 로깅되어 개인정보를 보호합니다
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// 캐시 성능 메트릭 수집 활성화 여부
    /// 히트율, 미스율, 응답 시간 등의 메트릭을 수집합니다
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// 캐시 이벤트 로깅 레벨
    /// Debug: 모든 캐시 작업 로깅
    /// Information: 중요한 이벤트만 로깅 (오류, 페일세이프 활성화 등)
    /// Warning: 경고 및 오류만 로깅
    /// Error: 오류만 로깅
    /// </summary>
    public LogLevel CacheEventLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// 메트릭 수집 간격 (초)
    /// 0으로 설정하면 실시간 수집, 최대 300초(5분)
    /// </summary>
    [Range(0, 300, ErrorMessage = "MetricsCollectionIntervalSeconds는 0 이상 300 이하여야 합니다.")]
    public int MetricsCollectionIntervalSeconds { get; set; } = 0;

    /// <summary>
    /// FusionCache 사용 여부를 결정하는 기능 플래그
    /// true: FusionCache 사용, false: 기존 Redis 캐시 사용
    /// </summary>
    public bool UseFusionCache { get; set; } = false;

    /// <summary>
    /// 점진적 전환을 위한 트래픽 비율 (0.0 ~ 1.0)
    /// 0.0: 모든 트래픽이 기존 구현체 사용
    /// 1.0: 모든 트래픽이 FusionCache 사용
    /// 0.5: 50% 트래픽이 FusionCache 사용
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "TrafficSplitRatio는 0.0 이상 1.0 이하여야 합니다.")]
    public double TrafficSplitRatio { get; set; } = 0.0;

    /// <summary>
    /// 트래픽 분할 시 사용할 해시 시드
    /// 동일한 IP에 대해 일관된 라우팅을 보장합니다
    /// </summary>
    public int TrafficSplitHashSeed { get; set; } = 12345;

    /// <summary>
    /// 설정 유효성을 검증합니다
    /// </summary>
    /// <returns>유효성 검증 결과와 오류 메시지</returns>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // HardTimeout이 SoftTimeout보다 큰지 확인
        if (HardTimeout <= SoftTimeout)
        {
            errors.Add("HardTimeout은 SoftTimeout보다 커야 합니다.");
        }

        // L1CacheDuration이 DefaultEntryOptions보다 작거나 같은지 확인
        if (L1CacheDuration > DefaultEntryOptions)
        {
            errors.Add("L1CacheDuration은 DefaultEntryOptions보다 작거나 같아야 합니다.");
        }

        // FailSafeMaxDuration이 DefaultEntryOptions보다 큰지 확인
        if (FailSafeMaxDuration <= DefaultEntryOptions)
        {
            errors.Add("FailSafeMaxDuration은 DefaultEntryOptions보다 커야 합니다.");
        }

        // Redis 설정이 있는 경우 연결 문자열 확인
        if (Redis != null && string.IsNullOrWhiteSpace(Redis.IpToNationConnectionString))
        {
            errors.Add("Redis.IpToNationConnectionString이 설정되지 않았습니다.");
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// 설정 값들을 로그에 안전하게 출력하기 위한 문자열 표현
    /// 민감한 정보는 마스킹됩니다
    /// </summary>
    /// <returns>설정 정보 문자열</returns>
    public override string ToString()
    {
        var connectionString = string.IsNullOrEmpty(ConnectionString) 
            ? "Not configured" 
            : $"{ConnectionString[..Math.Min(10, ConnectionString.Length)]}***";
            
        return $"FusionCache Config - " +
               $"DefaultTTL: {DefaultEntryOptions}, " +
               $"L1Duration: {L1CacheDuration}, " +
               $"L1MaxSize: {L1CacheMaxSize}, " +
               $"SoftTimeout: {SoftTimeout}, " +
               $"HardTimeout: {HardTimeout}, " +
               $"FailSafe: {EnableFailSafe}, " +
               $"EagerRefresh: {EnableEagerRefresh}, " +
               $"ConnectionString: {connectionString}, " +
               $"KeyPrefix: {KeyPrefix}";
    }
}