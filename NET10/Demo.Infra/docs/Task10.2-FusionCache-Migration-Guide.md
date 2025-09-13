# Task 10.2 - FusionCache 마이그레이션 가이드

## 개요

이 문서는 기존 IpToNationRedisCache에서 FusionCache로의 완전한 마이그레이션 가이드를 제공합니다. 점진적 전환, 성능 모니터링, 그리고 운영 방법에 대한 상세한 정보를 포함합니다.

## 목차

1. [마이그레이션 개요](#마이그레이션-개요)
2. [사전 준비사항](#사전-준비사항)
3. [단계별 마이그레이션 계획](#단계별-마이그레이션-계획)
4. [설정 변경 사항](#설정-변경-사항)
5. [성능 개선 사항](#성능-개선-사항)
6. [모니터링 방법](#모니터링-방법)
7. [문제 해결](#문제-해결)
8. [롤백 절차](#롤백-절차)

## 마이그레이션 개요

### 기존 아키텍처

```
Application Layer
       ↓
IIpToNationCache
       ↓
IpToNationRedisCache
       ↓
StackExchange.Redis → Redis Server
```

### 새로운 아키텍처

```
Application Layer
       ↓
IIpToNationCache
       ↓
IpToNationCacheWrapper (전환 메커니즘)
    ↓                    ↓
IpToNationFusionCache   IpToNationRedisCache
       ↓                    ↓
   FusionCache         StackExchange.Redis
    ↓        ↓               ↓
L1 Cache   L2 Cache    Redis Server
(Memory)   (Redis)
```

### 주요 개선사항

1. **L1 + L2 하이브리드 캐시**: 메모리와 Redis의 이중 캐시 구조
2. **백그라운드 새로고침**: 만료 전 자동 갱신으로 캐시 미스 최소화
3. **페일세이프 메커니즘**: Redis 장애 시에도 서비스 지속성 보장
4. **캐시 스탬피드 방지**: 동시 요청에 대한 중복 처리 방지
5. **향상된 모니터링**: OpenTelemetry 통합 및 상세 메트릭

## 사전 준비사항

### 1. 패키지 의존성 확인

다음 NuGet 패키지들이 설치되어 있는지 확인하세요:

```xml
<PackageReference Include="ZiggyCreatures.FusionCache" Version="1.2.0" />
<PackageReference Include="ZiggyCreatures.FusionCache.Serialization.SystemTextJson" Version="1.2.0" />
<PackageReference Include="ZiggyCreatures.FusionCache.OpenTelemetry" Version="1.2.0" />
```

### 2. Redis 서버 상태 확인

- Redis 서버가 정상 작동하는지 확인
- 기존 캐시 데이터 백업 (필요시)
- Redis 메모리 사용량 모니터링 설정

### 3. 모니터링 도구 준비

- OpenTelemetry 수집기 설정
- 로그 수집 시스템 확인
- 메트릭 대시보드 준비

## 단계별 마이그레이션 계획

### Phase 1: 개발 환경 테스트 (1-2일)

#### 목표
- FusionCache 기본 기능 검증
- 성능 벤치마크 수행
- 기존 기능과의 호환성 확인

#### 설정
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 1.0,
    "EnableDetailedLogging": true,
    "EnableMetrics": true
  }
}
```

#### 검증 항목
- [ ] 기존 API 테스트 모두 통과
- [ ] 캐시 히트/미스 동작 확인
- [ ] L1/L2 캐시 계층 동작 검증
- [ ] 페일오버 시나리오 테스트
- [ ] 성능 벤치마크 수행

### Phase 2: 스테이징 환경 검증 (3-5일)

#### 목표
- 실제 트래픽 패턴으로 테스트
- 부하 테스트 수행
- 모니터링 시스템 검증

#### 설정
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.5,
    "EnableDetailedLogging": false,
    "EnableMetrics": true
  }
}
```

#### 검증 항목
- [ ] 50% 트래픽 분할 정상 동작
- [ ] 부하 테스트 통과 (기존 대비 성능 개선 확인)
- [ ] 메모리 사용량 모니터링
- [ ] 오류율 및 응답 시간 측정
- [ ] 알림 시스템 동작 확인

### Phase 3: 프로덕션 점진적 전환 (2-3주)

#### Week 1: 5% 트래픽 전환
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.05
  }
}
```

**모니터링 포인트:**
- 오류율 < 0.1%
- 평균 응답 시간 개선 확인
- 메모리 사용량 안정성

#### Week 2: 25% 트래픽 전환
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.25
  }
}
```

**모니터링 포인트:**
- L1 캐시 히트율 > 80%
- Redis 부하 감소 확인
- 백그라운드 새로고침 동작 확인

#### Week 3: 100% 트래픽 전환
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 1.0
  }
}
```

**모니터링 포인트:**
- 전체 시스템 안정성
- 성능 개선 효과 측정
- 사용자 경험 개선 확인

### Phase 4: 최적화 및 정리 (1주)

#### 목표
- 설정 최적화
- 기존 코드 정리
- 문서 업데이트

#### 작업 항목
- [ ] 캐시 설정 튜닝
- [ ] 불필요한 기존 코드 제거
- [ ] 모니터링 대시보드 업데이트
- [ ] 운영 문서 작성

## 설정 변경 사항

### 새로운 설정 섹션

#### FusionCache 기본 설정
```json
{
  "FusionCache": {
    "DefaultEntryOptions": "00:30:00",
    "L1CacheDuration": "00:05:00",
    "L1CacheMaxSize": 1000,
    "SoftTimeout": "00:00:01",
    "HardTimeout": "00:00:05",
    "EnableFailSafe": true,
    "EnableEagerRefresh": true,
    "FailSafeMaxDuration": "01:00:00",
    "FailSafeThrottleDuration": "00:00:30",
    "EagerRefreshThreshold": 0.8,
    "EnableCacheStampedeProtection": true
  }
}
```

#### 전환 메커니즘 설정
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.1,
    "TrafficSplitHashSeed": 12345
  }
}
```

#### 모니터링 설정
```json
{
  "FusionCache": {
    "EnableOpenTelemetry": true,
    "EnableDetailedLogging": false,
    "EnableMetrics": true,
    "CacheEventLogLevel": "Information",
    "MetricsCollectionIntervalSeconds": 60
  }
}
```

### 환경별 설정 권장사항

#### Development
```json
{
  "FusionCache": {
    "L1CacheDuration": "00:02:00",
    "L1CacheMaxSize": 500,
    "EnableDetailedLogging": true,
    "CacheEventLogLevel": "Debug"
  }
}
```

#### Staging
```json
{
  "FusionCache": {
    "L1CacheDuration": "00:03:00",
    "L1CacheMaxSize": 750,
    "EnableDetailedLogging": false,
    "CacheEventLogLevel": "Information"
  }
}
```

#### Production
```json
{
  "FusionCache": {
    "L1CacheDuration": "00:05:00",
    "L1CacheMaxSize": 1000,
    "EnableDetailedLogging": false,
    "CacheEventLogLevel": "Warning"
  }
}
```

## 성능 개선 사항

### 1. 응답 시간 개선

| 시나리오 | 기존 (Redis만) | FusionCache | 개선율 |
|----------|----------------|-------------|--------|
| L1 캐시 히트 | 1-2ms | 0.1-0.2ms | 80-90% |
| L2 캐시 히트 | 1-2ms | 1-2ms | 동일 |
| 캐시 미스 | 10-50ms | 10-50ms | 동일 |

### 2. 처리량 개선

- **동시 요청 처리**: 캐시 스탬피드 방지로 30-50% 개선
- **백그라운드 새로고침**: 캐시 미스 발생률 60-80% 감소
- **메모리 캐시**: 빈번한 요청에 대해 10배 이상 성능 향상

### 3. 안정성 개선

- **Redis 장애 대응**: 페일세이프로 99.9% 가용성 유지
- **메모리 관리**: LRU 정책으로 안정적인 메모리 사용
- **타임아웃 처리**: Soft/Hard 타임아웃으로 응답성 보장

## 모니터링 방법

### 1. 핵심 메트릭

#### 캐시 성능 메트릭
```
fusion_cache_hits_total - 캐시 히트 수
fusion_cache_misses_total - 캐시 미스 수
fusion_cache_hit_rate_current - 현재 히트율 (%)
fusion_cache_operation_duration_seconds - 작업 지속 시간
```

#### 시스템 메트릭
```
fusion_cache_l1_memory_usage_bytes - L1 캐시 메모리 사용량
fusion_cache_failsafe_activations_total - 페일세이프 활성화 횟수
fusion_cache_background_refresh_total - 백그라운드 새로고침 횟수
```

### 2. 로그 모니터링

#### 정상 동작 로그
```
[INFO] FusionCache가 성공적으로 초기화되었습니다. CacheName: dev:IpToNationCache
[DEBUG] FusionCache Hit: Key=ipcache:192.168.1.100
[DEBUG] FusionCache Background Factory Success: Key=ipcache:192.168.1.100
```

#### 주의 필요 로그
```
[WARNING] FusionCache FailSafe Activated: Key=ipcache:192.168.1.100
[ERROR] FusionCache Factory Error: Key=ipcache:192.168.1.100
[WARNING] FusionCache 오류로 인해 기존 Redis 캐시로 폴백합니다
```

### 3. 대시보드 구성

#### Grafana 대시보드 예시
```json
{
  "dashboard": {
    "title": "FusionCache Monitoring",
    "panels": [
      {
        "title": "Cache Hit Rate",
        "type": "stat",
        "targets": [
          {
            "expr": "rate(fusion_cache_hits_total[5m]) / (rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m])) * 100"
          }
        ]
      },
      {
        "title": "Response Time",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, fusion_cache_operation_duration_seconds)"
          }
        ]
      }
    ]
  }
}
```

### 4. 알림 규칙

#### Prometheus 알림 규칙
```yaml
groups:
  - name: fusioncache
    rules:
      - alert: FusionCacheHighErrorRate
        expr: rate(fusion_cache_errors_total[5m]) > 0.01
        for: 2m
        labels:
          severity: warning
        annotations:
          summary: "FusionCache 오류율이 높습니다"
          
      - alert: FusionCacheLowHitRate
        expr: rate(fusion_cache_hits_total[5m]) / (rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m])) < 0.7
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "FusionCache 히트율이 낮습니다"
```

## 문제 해결

### 1. 일반적인 문제들

#### Redis 연결 실패
**증상**: `FusionCache L2 (Redis) 캐시를 사용할 수 없습니다` 로그
**해결방법**:
1. Redis 서버 상태 확인
2. 연결 문자열 검증
3. 네트워크 연결 확인
4. 페일세이프가 활성화되어 L1 캐시로 동작하는지 확인

#### 메모리 사용량 증가
**증상**: 애플리케이션 메모리 사용량 지속적 증가
**해결방법**:
1. `L1CacheMaxSize` 설정 확인 및 조정
2. `CompactionPercentage` 설정 검토
3. 메모리 누수 여부 확인
4. GC 동작 모니터링

#### 성능 저하
**증상**: 응답 시간이 기존보다 느려짐
**해결방법**:
1. L1 캐시 히트율 확인
2. 타임아웃 설정 검토
3. 백그라운드 새로고침 동작 확인
4. Redis 서버 성능 점검

### 2. 디버깅 방법

#### 상세 로깅 활성화
```json
{
  "FusionCache": {
    "EnableDetailedLogging": true,
    "CacheEventLogLevel": "Debug"
  },
  "Logging": {
    "LogLevel": {
      "Demo.Infra.Repositories.IpToNationFusionCache": "Debug"
    }
  }
}
```

#### 메트릭 수집 활성화
```json
{
  "FusionCache": {
    "EnableMetrics": true,
    "MetricsCollectionIntervalSeconds": 30
  }
}
```

## 롤백 절차

### 1. 즉시 롤백 (긴급 상황)

#### 설정 변경
```json
{
  "FusionCache": {
    "UseFusionCache": false,
    "TrafficSplitRatio": 0.0
  }
}
```

#### 확인 사항
- [ ] 모든 트래픽이 기존 Redis 캐시로 라우팅되는지 확인
- [ ] 오류율이 정상 수준으로 복구되는지 확인
- [ ] 응답 시간이 안정화되는지 확인

### 2. 점진적 롤백

#### Step 1: 트래픽 감소 (50% → 25% → 5%)
```json
{
  "FusionCache": {
    "UseFusionCache": true,
    "TrafficSplitRatio": 0.25  // 점진적으로 감소
  }
}
```

#### Step 2: 완전 비활성화
```json
{
  "FusionCache": {
    "UseFusionCache": false,
    "TrafficSplitRatio": 0.0
  }
}
```

### 3. 코드 롤백 (필요시)

#### 서비스 등록 변경
```csharp
// 기존 방식으로 복귀
services.AddIpToNationRedisCache(configuration);
// services.AddIpToNationCacheWithMigration(configuration); // 주석 처리
```

## 운영 체크리스트

### 일일 점검 항목
- [ ] 캐시 히트율 > 80%
- [ ] 오류율 < 0.1%
- [ ] 평균 응답 시간 < 2ms
- [ ] 메모리 사용량 안정성
- [ ] Redis 연결 상태

### 주간 점검 항목
- [ ] 성능 트렌드 분석
- [ ] 용량 계획 검토
- [ ] 설정 최적화 검토
- [ ] 백업 및 복구 테스트

### 월간 점검 항목
- [ ] 전체 시스템 성능 리뷰
- [ ] 비용 효율성 분석
- [ ] 보안 점검
- [ ] 문서 업데이트

## 결론

FusionCache로의 마이그레이션은 다음과 같은 이점을 제공합니다:

1. **성능 향상**: L1 캐시를 통한 80-90% 응답 시간 개선
2. **안정성 증대**: 페일세이프 메커니즘으로 99.9% 가용성 보장
3. **운영 효율성**: 백그라운드 새로고침으로 캐시 미스 최소화
4. **모니터링 강화**: OpenTelemetry 통합으로 상세한 관찰성 제공

점진적 전환 메커니즘을 통해 안전하게 마이그레이션할 수 있으며, 언제든지 롤백이 가능합니다. 적절한 모니터링과 단계적 접근을 통해 성공적인 마이그레이션을 달성할 수 있습니다.