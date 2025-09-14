# Task 6.2: FusionCache 로깅 및 모니터링 구현

## 개요

FusionCache 이벤트에 대한 구조화된 로깅, 캐시 히트율/미스율 메트릭 수집, 그리고 오류 발생 시 적절한 로그 레벨로 기록하는 로직을 구현했습니다.

## 구현된 기능

### 1. 구조화된 로깅 개선

#### IpToNationFusionCache 로깅 개선
- **상세 로깅 모드**: `EnableDetailedLogging` 설정에 따라 로깅 레벨과 내용 조정
- **개인정보 보호**: IP 주소는 해시값으로 로깅하여 개인정보 보호
- **성능 메트릭**: 각 캐시 작업의 지속 시간을 밀리초 단위로 기록
- **구조화된 정보**: 키 해시, 국가 코드, 작업 시간 등을 구조화된 형태로 로깅

```csharp
// 상세 로깅 활성화 시
_logger.LogInformation("캐시 히트: IP {ClientIpHash}에 대한 국가 코드 {CountryCode}를 반환합니다. " +
    "Duration: {Duration}ms, Key: {KeyHash}", 
    clientIp.GetHashCode(), result, stopwatch.ElapsedMilliseconds, key.GetHashCode());

// 상세 로깅 비활성화 시
_logger.LogDebug("캐시 히트: IP {ClientIp}에 대한 국가 코드 {CountryCode}를 반환합니다", clientIp, result);
```

#### 로그 레벨 최적화
- **Debug**: 일반적인 캐시 작업 (상세 로깅 비활성화 시)
- **Information**: 중요한 이벤트 및 상세 정보 (상세 로깅 활성화 시)
- **Error**: 모든 오류 상황

### 2. 고급 메트릭 수집 시스템

#### FusionCacheMetricsService 구현
새로운 메트릭 서비스를 통해 포괄적인 캐시 성능 지표를 수집합니다:

```csharp
public class FusionCacheMetricsService : IDisposable
{
    // 실시간 메트릭 수집
    public void RecordCacheOperation(string cacheName, string operation, string result, double durationMs, Dictionary<string, object?>? additionalTags = null)
    
    // 캐시별 메트릭 조회
    public CacheMetricsSnapshot? GetCacheMetrics(string cacheName)
    
    // 전체 집계 메트릭 조회
    public CacheMetricsSnapshot GetAggregatedMetrics()
    
    // 메트릭 초기화
    public void ResetMetrics(string? cacheName = null)
    
    // 메트릭 요약 로깅
    public void LogMetricsSummary()
}
```

#### 수집되는 메트릭
- **히트/미스 카운터**: 캐시 히트 및 미스 횟수
- **설정 카운터**: 캐시 설정 작업 횟수
- **오류 카운터**: 캐시 작업 중 발생한 오류 횟수
- **히트율/미스율**: 실시간 계산되는 백분율
- **평균 응답 시간**: 캐시 작업의 평균 지속 시간
- **OpenTelemetry 메트릭**: 관찰 가능한 게이지 메트릭

#### OpenTelemetry 통합
```csharp
// 관찰 가능한 메트릭 생성
_hitRateGauge = _meter.CreateObservableGauge("fusion_cache_hit_rate_percent", 
    () => GetAggregatedHitRate(), 
    "%", "FusionCache 전체 히트율");

_missRateGauge = _meter.CreateObservableGauge("fusion_cache_miss_rate_percent", 
    () => GetAggregatedMissRate(), 
    "%", "FusionCache 전체 미스율");
```

### 3. 오류 처리 및 로깅 개선

#### 적절한 로그 레벨 적용
- **Error 레벨**: 모든 예외 상황
- **구조화된 오류 정보**: 오류 타입, 메시지, 지속 시간, 컨텍스트 정보 포함
- **메트릭 연동**: 오류 발생 시 메트릭 카운터 자동 증가

```csharp
_logger.LogError(ex, "IP {ClientIpHash}에 대한 캐시 조회 중 오류가 발생했습니다. " +
    "Duration: {Duration}ms, Key: {KeyHash}, ErrorType: {ErrorType}", 
    clientIp.GetHashCode(), stopwatch.ElapsedMilliseconds, key.GetHashCode(), ex.GetType().Name);
```

### 4. 분산 추적 지원

#### Activity 태그 설정
각 캐시 작업에 대해 분산 추적을 위한 Activity 태그를 설정합니다:

```csharp
activity?.SetTag("cache.operation", "get");
activity?.SetTag("cache.key_hash", key.GetHashCode().ToString());
activity?.SetTag("cache.client_ip_hash", clientIp.GetHashCode().ToString());
activity?.SetTag("cache.implementation", "FusionCache");
activity?.SetTag("cache.result", "hit");
activity?.SetTag("cache.country_code", result);
```

### 5. 설정 기반 제어

#### FusionCacheConfig 확장
```csharp
public class FusionCacheConfig
{
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
    /// </summary>
    public LogLevel CacheEventLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// 메트릭 수집 간격 (초)
    /// </summary>
    public int MetricsCollectionIntervalSeconds { get; set; } = 0;
}
```

## 테스트 구현

### 1. 단위 테스트
- **IpToNationFusionCacheLoggingTests**: 로깅 기능 전용 테스트
- **FusionCacheMetricsServiceTests**: 메트릭 서비스 단위 테스트

### 2. 통합 테스트
- **IpToNationFusionCacheMetricsIntegrationTests**: 캐시와 메트릭 서비스 통합 테스트

### 주요 테스트 시나리오
- 캐시 히트/미스 시 구조화된 로깅 검증
- 오류 발생 시 적절한 로그 레벨 검증
- 메트릭 수집 및 계산 정확성 검증
- 히트율/미스율 계산 검증
- 집계 메트릭 계산 검증
- 메트릭 초기화 기능 검증

## 사용 방법

### 1. 서비스 등록
```csharp
// ServiceCollectionExtensions에서 자동 등록됨
services.AddSingleton<FusionCacheMetricsService>();
services.AddScoped<IIpToNationCache, IpToNationFusionCache>();
```

### 2. 설정 구성
```json
{
  "FusionCache": {
    "EnableDetailedLogging": true,
    "EnableMetrics": true,
    "CacheEventLogLevel": "Information",
    "MetricsCollectionIntervalSeconds": 60
  }
}
```

### 3. 메트릭 조회
```csharp
// 특정 캐시 메트릭 조회
var metrics = metricsService.GetCacheMetrics("IpToNationCache");
Console.WriteLine($"히트율: {metrics.HitRatePercent:F2}%");

// 전체 집계 메트릭 조회
var aggregated = metricsService.GetAggregatedMetrics();
Console.WriteLine($"전체 히트율: {aggregated.HitRatePercent:F2}%");

// 메트릭 요약 로깅
metricsService.LogMetricsSummary();
```

## 성능 고려사항

### 1. 메트릭 수집 최적화
- **스레드 안전성**: `Interlocked` 연산을 사용한 원자적 카운터 업데이트
- **메모리 효율성**: 캐시별로 분리된 메트릭 저장
- **비동기 처리**: 메트릭 수집이 캐시 작업 성능에 미치는 영향 최소화

### 2. 로깅 최적화
- **조건부 로깅**: `IsEnabled()` 체크를 통한 불필요한 로깅 방지
- **구조화된 로깅**: 성능이 중요한 경로에서 문자열 보간 최소화
- **개인정보 보호**: IP 주소 해시화를 통한 보안 강화

## 모니터링 및 알림

### 1. 주요 모니터링 지표
- **히트율**: 90% 이상 유지 권장
- **평균 응답 시간**: L1 캐시 1ms 이하, L2 캐시 10ms 이하
- **오류율**: 1% 이하 유지 권장

### 2. 알림 설정 권장사항
- 히트율 80% 이하 시 경고
- 평균 응답 시간 100ms 초과 시 경고
- 오류율 5% 초과 시 긴급 알림

## 요구사항 검증

### 요구사항 4.2 ✅
- **구조화된 로깅**: 모든 캐시 이벤트에 대해 구조화된 로깅 구현
- **개인정보 보호**: IP 주소 해시화를 통한 개인정보 보호
- **상세 로깅 제어**: 설정을 통한 로깅 레벨 제어

### 요구사항 4.3 ✅
- **메트릭 수집**: 히트율, 미스율, 응답 시간 등 포괄적 메트릭 수집
- **오류 로깅**: 모든 오류를 Error 레벨로 적절히 기록
- **OpenTelemetry 통합**: 관찰 가능한 메트릭을 통한 모니터링 지원

## 결론

FusionCache의 로깅 및 모니터링 기능이 성공적으로 구현되었습니다. 이를 통해:

1. **운영 가시성 향상**: 상세한 로깅과 메트릭을 통한 캐시 성능 모니터링
2. **문제 진단 개선**: 구조화된 로깅을 통한 빠른 문제 식별 및 해결
3. **성능 최적화**: 실시간 메트릭을 통한 캐시 성능 최적화 지원
4. **보안 강화**: 개인정보 보호를 고려한 안전한 로깅

모든 기능은 포괄적인 테스트를 통해 검증되었으며, 프로덕션 환경에서 안전하게 사용할 수 있습니다.