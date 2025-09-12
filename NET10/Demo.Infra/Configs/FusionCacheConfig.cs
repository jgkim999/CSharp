using Demo.Application.Configs;
using Microsoft.Extensions.Logging;

namespace Demo.Infra.Configs;

/// <summary>
/// FusionCache 설정을 위한 구성 클래스
/// L1(메모리) + L2(Redis) 하이브리드 캐시 옵션을 정의합니다
/// 기존 RedisConfig와 통합하여 Redis 연결 설정을 재사용합니다
/// </summary>
public class FusionCacheConfig
{
    /// <summary>
    /// Redis 설정 (기존 RedisConfig와 호환성 유지)
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
    /// </summary>
    public TimeSpan DefaultEntryOptions { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// L1 메모리 캐시 지속 시간
    /// </summary>
    public TimeSpan L1CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 소프트 타임아웃 (백그라운드에서 계속 시도)
    /// </summary>
    public TimeSpan SoftTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 하드 타임아웃 (완전 중단)
    /// </summary>
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
    /// </summary>
    public TimeSpan FailSafeMaxDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// 페일세이프 스로틀 지속 시간
    /// 연속적인 실패 시 재시도 간격
    /// </summary>
    public TimeSpan FailSafeThrottleDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 백그라운드 새로고침 임계점
    /// 만료 시간의 몇 퍼센트에서 새로고침을 시작할지 설정 (0.0 ~ 1.0)
    /// </summary>
    public float EagerRefreshThreshold { get; set; } = 0.8f;

    /// <summary>
    /// L1 캐시 최대 항목 수
    /// 메모리 사용량 제한
    /// </summary>
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
    /// 0으로 설정하면 실시간 수집
    /// </summary>
    public int MetricsCollectionIntervalSeconds { get; set; } = 0;
}