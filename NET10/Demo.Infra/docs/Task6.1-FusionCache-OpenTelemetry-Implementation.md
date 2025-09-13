# Task 6.1 - FusionCache OpenTelemetry 계측 설정 구현

## 개요

FusionCache의 OpenTelemetry 확장 패키지를 추가하고, 캐시 작업에 대한 메트릭 수집 및 분산 추적을 구현했습니다. 이를 통해 캐시 성능 모니터링과 분산 시스템에서의 추적이 가능해졌습니다.

## 구현 내용

### 1. 패키지 추가

```bash
dotnet add Demo.Infra/Demo.Infra.csproj package ZiggyCreatures.FusionCache.OpenTelemetry
```

**추가된 패키지:**
- `ZiggyCreatures.FusionCache.OpenTelemetry` (2.4.0)

### 2. ServiceCollectionExtensions 업데이트

**파일:** `Demo.Infra/Extensions/ServiceCollectionExtensions.cs`

#### 2.1 필요한 네임스페이스 추가

```csharp
using ZiggyCreatures.Caching.Fusion.OpenTelemetry;
using System.Diagnostics;
using System.Diagnostics.Metrics;
```

#### 2.2 OpenTelemetry 계측 메서드 개선

```csharp
/// <summary>
/// FusionCache OpenTelemetry 계측을 설정합니다
/// 캐시 작업에 대한 메트릭과 추적을 제공합니다
/// Redis 인스트루멘테이션과 연동하여 분산 추적을 지원합니다
/// </summary>
private static void SetupOpenTelemetryInstrumentation(FusionCache fusionCache, ILogger logger)
{
    // FusionCache OpenTelemetry 확장을 사용하여 자동 계측 활성화
    fusionCache.AddOpenTelemetry();

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

    // 이벤트 핸들러를 통한 메트릭 수집 및 분산 추적 태그 설정
    // ... (상세 구현은 코드 참조)
}
```

### 3. OpenTelemetry 메트릭 등록

**파일:** `Demo.Web/AppInitializer.cs`

```csharp
// 메트릭 설정에 FusionCache 메트릭 추가
metrics.AddMeter("Demo.Infra.FusionCache");
```

### 4. 애플리케이션 설정 업데이트

**파일:** `Demo.Web/Program.cs`

```csharp
// FusionCache 서비스 등록
builder.Services.AddIpToNationFusionCache(builder.Configuration);
```

**필요한 using 추가:**
```csharp
using Demo.Infra.Extensions;
```

### 5. 설정 파일 구성

**파일:** `Demo.Web/appsettings.json`

```json
{
  "FusionCache": {
    "EnableOpenTelemetry": true,
    "EnableDetailedLogging": false,
    // ... 기타 FusionCache 설정
  }
}
```

## 수집되는 메트릭

### 1. 카운터 메트릭

| 메트릭 이름 | 설명 | 태그 |
|------------|------|------|
| `fusion_cache_hits_total` | 캐시 히트 총 횟수 | `cache_name` |
| `fusion_cache_misses_total` | 캐시 미스 총 횟수 | `cache_name` |
| `fusion_cache_sets_total` | 캐시 설정 총 횟수 | `cache_name` |
| `fusion_cache_failsafe_activations_total` | 페일세이프 활성화 총 횟수 | `cache_name` |
| `fusion_cache_errors_total` | 캐시 오류 총 횟수 | `cache_name`, `error_type` |

### 2. 분산 추적 태그

| 태그 이름 | 설명 | 예시 값 |
|----------|------|---------|
| `fusion_cache.operation` | 캐시 작업 유형 | `hit`, `miss`, `set`, `failsafe_activate` |
| `fusion_cache.cache_name` | 캐시 인스턴스 이름 | `dev:IpToNationCache` |
| `fusion_cache.key_hash` | 캐시 키의 해시값 | `123456789` |
| `fusion_cache.error` | 오류 발생 여부 | `true`, `false` |

## 테스트 구현

**파일:** `Demo.Infra.Tests/Repositories/IpToNationFusionCacheOpenTelemetryTests.cs`

### 테스트 케이스

1. **Activity 생성 및 태그 설정 테스트**
   - 캐시 작업 시 올바른 Activity가 생성되는지 검증
   - 분산 추적 태그가 올바르게 설정되는지 확인

2. **메트릭 수집 테스트**
   - 캐시 히트/미스 시 해당 메트릭이 기록되는지 검증
   - 캐시 설정 작업 시 메트릭이 기록되는지 확인

3. **구성 검증 테스트**
   - FusionCache가 OpenTelemetry와 함께 올바르게 구성되었는지 확인
   - 페일세이프 설정이 활성화되었는지 검증

4. **다중 작업 추적 테스트**
   - 여러 캐시 작업에 대해 각각 별도의 추적이 생성되는지 확인

## 모니터링 및 관찰성

### 1. 메트릭 대시보드

다음 메트릭을 사용하여 Grafana 대시보드를 구성할 수 있습니다:

```promql
# 캐시 히트율
rate(fusion_cache_hits_total[5m]) / (rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m])) * 100

# 캐시 작업 처리량
rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m]) + rate(fusion_cache_sets_total[5m])

# 페일세이프 활성화율
rate(fusion_cache_failsafe_activations_total[5m])

# 오류율
rate(fusion_cache_errors_total[5m])
```

### 2. 알림 규칙

```yaml
# 캐시 히트율이 낮을 때 알림
- alert: LowCacheHitRate
  expr: |
    (
      rate(fusion_cache_hits_total[5m]) / 
      (rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m]))
    ) * 100 < 70
  for: 2m
  labels:
    severity: warning
  annotations:
    summary: "FusionCache 히트율이 낮습니다"
    description: "캐시 히트율이 {{ $value }}%로 70% 미만입니다"

# 페일세이프가 자주 활성화될 때 알림
- alert: FrequentFailsafeActivation
  expr: rate(fusion_cache_failsafe_activations_total[5m]) > 0.1
  for: 1m
  labels:
    severity: critical
  annotations:
    summary: "FusionCache 페일세이프가 자주 활성화됩니다"
    description: "페일세이프 활성화율이 {{ $value }}/초입니다"
```

### 3. 분산 추적

Jaeger나 Zipkin에서 다음과 같은 추적 정보를 확인할 수 있습니다:

- HTTP 요청 → 캐시 조회 → Redis 작업의 전체 플로우
- 캐시 히트/미스에 따른 성능 차이
- 페일세이프 활성화 시나리오
- 백그라운드 새로고침 작업

## 성능 영향

### 1. 오버헤드 최소화

- 메트릭 수집은 비동기적으로 수행됩니다
- Activity 태그는 현재 추적 컨텍스트가 있을 때만 설정됩니다
- 키 해시값을 사용하여 민감한 정보 노출을 방지합니다

### 2. 메모리 사용량

- 메트릭 카운터는 메모리 효율적입니다
- Activity는 자동으로 가비지 컬렉션됩니다
- Meter 인스턴스는 싱글톤으로 관리됩니다

## 문제 해결

### 1. 메트릭이 수집되지 않는 경우

```csharp
// appsettings.json에서 OpenTelemetry 활성화 확인
"FusionCache": {
  "EnableOpenTelemetry": true
}

// MeterProvider에 FusionCache 메터 등록 확인
metrics.AddMeter("Demo.Infra.FusionCache");
```

### 2. 분산 추적이 연결되지 않는 경우

```csharp
// Activity.Current가 null인지 확인
if (Activity.Current != null)
{
    Activity.Current.SetTag("fusion_cache.operation", "hit");
}
```

### 3. Redis 인스트루멘테이션과의 연동 문제

```csharp
// StackExchange.Redis 인스트루멘테이션이 활성화되어 있는지 확인
tracing.AddRedisInstrumentation();
```

## 다음 단계

1. **로깅 및 모니터링 구현** (Task 6.2)
   - 구조화된 로깅 추가
   - 캐시 성능 메트릭 확장
   - 오류 처리 개선

2. **대시보드 구성**
   - Grafana 대시보드 생성
   - 알림 규칙 설정
   - SLA 모니터링 구현

3. **성능 최적화**
   - 메트릭 수집 오버헤드 분석
   - 샘플링 전략 구현
   - 배치 처리 최적화

## 요구사항 충족 확인

✅ **요구사항 4.1**: FusionCache 작업에 대한 OpenTelemetry 메트릭 수집 구현  
✅ **요구사항 4.4**: 분산 추적에 FusionCache 작업 포함 구현  

- FusionCache OpenTelemetry 확장 패키지 추가 완료
- 캐시 작업에 대한 메트릭 수집 설정 구현 완료
- 분산 추적 태그 설정으로 추적 연동 구현 완료
- Redis 인스트루멘테이션과의 연동 구현 완료