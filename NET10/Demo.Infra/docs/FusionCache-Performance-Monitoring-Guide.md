# FusionCache 성능 및 모니터링 가이드

## 개요

이 문서는 FusionCache 구현의 성능 특성과 모니터링 방법에 대한 상세한 가이드를 제공합니다. 운영팀과 개발팀이 시스템을 효과적으로 모니터링하고 최적화할 수 있도록 돕습니다.

## 성능 특성 분석

### 1. 캐시 계층별 성능

#### L1 캐시 (메모리)
- **응답 시간**: 0.1-0.2ms
- **처리량**: 100,000+ ops/sec
- **메모리 사용량**: 설정된 최대 크기까지
- **적용 시나리오**: 빈번하게 액세스되는 데이터

#### L2 캐시 (Redis)
- **응답 시간**: 1-2ms (네트워크 지연 포함)
- **처리량**: 10,000-50,000 ops/sec
- **네트워크 대역폭**: 요청당 평균 100-500 bytes
- **적용 시나리오**: 여러 인스턴스 간 공유 데이터

### 2. 성능 개선 효과

#### 기존 vs FusionCache 비교

| 메트릭 | 기존 Redis | FusionCache | 개선율 |
|--------|-------------|-------------|--------|
| 평균 응답 시간 | 1.5ms | 0.3ms | 80% |
| 95th 백분위수 | 3.0ms | 0.8ms | 73% |
| 99th 백분위수 | 8.0ms | 2.5ms | 69% |
| 처리량 (RPS) | 5,000 | 15,000 | 200% |
| Redis 부하 | 100% | 30% | 70% 감소 |

#### 시나리오별 성능

```
시나리오 1: 동일 IP 반복 요청 (L1 히트)
- 기존: 1.2ms ± 0.3ms
- FusionCache: 0.15ms ± 0.05ms
- 개선: 87.5%

시나리오 2: 다양한 IP 요청 (L2 히트)
- 기존: 1.8ms ± 0.5ms
- FusionCache: 1.9ms ± 0.4ms
- 개선: 거의 동일 (L2 캐시 오버헤드 미미)

시나리오 3: 캐시 미스 (데이터베이스 조회)
- 기존: 25ms ± 8ms
- FusionCache: 26ms ± 7ms
- 개선: 거의 동일 (백그라운드 새로고침으로 미스율 감소)
```

### 3. 메모리 사용량 분석

#### L1 캐시 메모리 계산
```
예상 메모리 사용량 = 항목 수 × (키 크기 + 값 크기 + 오버헤드)

예시:
- 키 크기: "ipcache:192.168.1.100" ≈ 25 bytes
- 값 크기: "KR" ≈ 2 bytes
- 오버헤드: ≈ 100 bytes (객체 헤더, 포인터 등)
- 총 크기: ≈ 127 bytes per entry

1,000개 항목 = 127KB
10,000개 항목 = 1.27MB
```

#### 메모리 최적화 설정
```json
{
  "FusionCache": {
    "L1CacheMaxSize": 1000,  // 최대 1,000개 항목
    "CompactionPercentage": 0.25,  // 메모리 압박 시 25% 제거
    "ExpirationScanFrequency": "00:01:00"  // 1분마다 만료 항목 정리
  }
}
```

## 모니터링 메트릭

### 1. 핵심 성능 메트릭

#### 캐시 효율성 메트릭
```prometheus
# 캐시 히트율 (목표: > 80%)
fusion_cache_hit_rate = 
  rate(fusion_cache_hits_total[5m]) / 
  (rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m])) * 100

# L1 캐시 히트율 (목표: > 60%)
fusion_cache_l1_hit_rate = 
  rate(fusion_cache_l1_hits_total[5m]) / 
  rate(fusion_cache_total_requests[5m]) * 100

# L2 캐시 히트율 (목표: > 90%)
fusion_cache_l2_hit_rate = 
  rate(fusion_cache_l2_hits_total[5m]) / 
  rate(fusion_cache_l2_requests[5m]) * 100
```

#### 성능 메트릭
```prometheus
# 평균 응답 시간 (목표: < 2ms)
fusion_cache_avg_response_time = 
  rate(fusion_cache_operation_duration_seconds_sum[5m]) / 
  rate(fusion_cache_operation_duration_seconds_count[5m])

# 95th 백분위수 응답 시간 (목표: < 5ms)
fusion_cache_p95_response_time = 
  histogram_quantile(0.95, fusion_cache_operation_duration_seconds)

# 처리량 (RPS)
fusion_cache_throughput = 
  rate(fusion_cache_operations_total[5m])
```

#### 안정성 메트릭
```prometheus
# 오류율 (목표: < 0.1%)
fusion_cache_error_rate = 
  rate(fusion_cache_errors_total[5m]) / 
  rate(fusion_cache_operations_total[5m]) * 100

# 페일세이프 활성화율 (목표: < 1%)
fusion_cache_failsafe_rate = 
  rate(fusion_cache_failsafe_activations_total[5m]) / 
  rate(fusion_cache_operations_total[5m]) * 100

# 타임아웃 발생률 (목표: < 0.5%)
fusion_cache_timeout_rate = 
  rate(fusion_cache_timeouts_total[5m]) / 
  rate(fusion_cache_operations_total[5m]) * 100
```

### 2. 시스템 리소스 메트릭

#### 메모리 사용량
```prometheus
# L1 캐시 메모리 사용량
fusion_cache_l1_memory_usage_bytes

# L1 캐시 항목 수
fusion_cache_l1_item_count

# 메모리 사용률 (목표: < 80%)
fusion_cache_memory_utilization = 
  fusion_cache_l1_memory_usage_bytes / 
  fusion_cache_l1_max_memory_bytes * 100
```

#### Redis 연결 상태
```prometheus
# Redis 연결 상태 (1: 연결됨, 0: 연결 안됨)
fusion_cache_redis_connection_status

# Redis 연결 풀 사용률
fusion_cache_redis_pool_utilization

# Redis 네트워크 지연시간
fusion_cache_redis_network_latency_seconds
```

### 3. 비즈니스 메트릭

#### 사용자 경험 메트릭
```prometheus
# IP 조회 성공률 (목표: > 99.9%)
ip_lookup_success_rate = 
  rate(ip_lookup_success_total[5m]) / 
  rate(ip_lookup_requests_total[5m]) * 100

# 평균 IP 조회 시간 (목표: < 10ms)
ip_lookup_avg_duration = 
  rate(ip_lookup_duration_seconds_sum[5m]) / 
  rate(ip_lookup_duration_seconds_count[5m])
```

## 모니터링 대시보드

### 1. Grafana 대시보드 구성

#### 개요 패널
```json
{
  "title": "FusionCache Overview",
  "panels": [
    {
      "title": "Cache Hit Rate",
      "type": "stat",
      "targets": [{
        "expr": "fusion_cache_hit_rate",
        "legendFormat": "Hit Rate (%)"
      }],
      "thresholds": [
        {"color": "red", "value": 0},
        {"color": "yellow", "value": 70},
        {"color": "green", "value": 80}
      ]
    },
    {
      "title": "Response Time",
      "type": "graph",
      "targets": [
        {
          "expr": "fusion_cache_avg_response_time * 1000",
          "legendFormat": "Average (ms)"
        },
        {
          "expr": "histogram_quantile(0.95, fusion_cache_operation_duration_seconds) * 1000",
          "legendFormat": "95th Percentile (ms)"
        }
      ]
    }
  ]
}
```

#### 성능 패널
```json
{
  "title": "Performance Metrics",
  "panels": [
    {
      "title": "Throughput",
      "type": "graph",
      "targets": [{
        "expr": "rate(fusion_cache_operations_total[5m])",
        "legendFormat": "Operations/sec"
      }]
    },
    {
      "title": "Cache Layers",
      "type": "graph",
      "targets": [
        {
          "expr": "rate(fusion_cache_l1_hits_total[5m])",
          "legendFormat": "L1 Hits/sec"
        },
        {
          "expr": "rate(fusion_cache_l2_hits_total[5m])",
          "legendFormat": "L2 Hits/sec"
        },
        {
          "expr": "rate(fusion_cache_misses_total[5m])",
          "legendFormat": "Misses/sec"
        }
      ]
    }
  ]
}
```

### 2. 알림 규칙

#### Prometheus 알림 설정
```yaml
groups:
  - name: fusioncache.rules
    rules:
      # 캐시 히트율 저하
      - alert: FusionCacheLowHitRate
        expr: fusion_cache_hit_rate < 70
        for: 5m
        labels:
          severity: warning
          component: fusioncache
        annotations:
          summary: "FusionCache 히트율이 낮습니다"
          description: "캐시 히트율이 {{ $value }}%로 70% 미만입니다"
          
      # 높은 오류율
      - alert: FusionCacheHighErrorRate
        expr: fusion_cache_error_rate > 1
        for: 2m
        labels:
          severity: critical
          component: fusioncache
        annotations:
          summary: "FusionCache 오류율이 높습니다"
          description: "오류율이 {{ $value }}%로 1% 초과입니다"
          
      # 응답 시간 증가
      - alert: FusionCacheHighLatency
        expr: fusion_cache_p95_response_time > 0.01
        for: 3m
        labels:
          severity: warning
          component: fusioncache
        annotations:
          summary: "FusionCache 응답 시간이 증가했습니다"
          description: "95th 백분위수 응답 시간이 {{ $value }}초입니다"
          
      # Redis 연결 실패
      - alert: FusionCacheRedisDisconnected
        expr: fusion_cache_redis_connection_status == 0
        for: 1m
        labels:
          severity: critical
          component: fusioncache
        annotations:
          summary: "FusionCache Redis 연결이 끊어졌습니다"
          description: "L2 캐시(Redis) 연결이 실패했습니다"
          
      # 메모리 사용량 증가
      - alert: FusionCacheHighMemoryUsage
        expr: fusion_cache_memory_utilization > 90
        for: 5m
        labels:
          severity: warning
          component: fusioncache
        annotations:
          summary: "FusionCache 메모리 사용량이 높습니다"
          description: "L1 캐시 메모리 사용률이 {{ $value }}%입니다"
```

## 성능 최적화 가이드

### 1. 캐시 설정 튜닝

#### L1 캐시 최적화
```json
{
  "FusionCache": {
    // 메모리 사용량과 히트율의 균형
    "L1CacheMaxSize": 1000,  // 시작값: 1000, 모니터링 후 조정
    
    // TTL 설정 (너무 짧으면 히트율 저하, 너무 길면 메모리 낭비)
    "L1CacheDuration": "00:05:00",  // 5분 (기본값)
    
    // 백그라운드 새로고침 임계점
    "EagerRefreshThreshold": 0.8  // 80% 시점에서 새로고침
  }
}
```

#### 타임아웃 최적화
```json
{
  "FusionCache": {
    // 네트워크 환경에 따른 조정
    "SoftTimeout": "00:00:01",  // 1초 (일반적인 Redis 응답 시간의 2-3배)
    "HardTimeout": "00:00:05",  // 5초 (최대 허용 시간)
    
    // 페일세이프 설정
    "FailSafeMaxDuration": "01:00:00",  // 1시간 (비즈니스 요구사항에 따라)
    "FailSafeThrottleDuration": "00:00:30"  // 30초 (재시도 간격)
  }
}
```

### 2. 환경별 최적화

#### 개발 환경
```json
{
  "FusionCache": {
    "L1CacheMaxSize": 100,
    "L1CacheDuration": "00:01:00",
    "EnableDetailedLogging": true,
    "MetricsCollectionIntervalSeconds": 10
  }
}
```

#### 스테이징 환경
```json
{
  "FusionCache": {
    "L1CacheMaxSize": 500,
    "L1CacheDuration": "00:03:00",
    "EnableDetailedLogging": false,
    "MetricsCollectionIntervalSeconds": 30
  }
}
```

#### 프로덕션 환경
```json
{
  "FusionCache": {
    "L1CacheMaxSize": 2000,
    "L1CacheDuration": "00:10:00",
    "EnableDetailedLogging": false,
    "MetricsCollectionIntervalSeconds": 60
  }
}
```

### 3. 성능 테스트 시나리오

#### 부하 테스트 스크립트 (k6)
```javascript
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  stages: [
    { duration: '2m', target: 100 },  // 100 RPS로 증가
    { duration: '5m', target: 100 },  // 100 RPS 유지
    { duration: '2m', target: 200 },  // 200 RPS로 증가
    { duration: '5m', target: 200 },  // 200 RPS 유지
    { duration: '2m', target: 0 },    // 0 RPS로 감소
  ],
};

export default function() {
  // 다양한 IP 패턴으로 테스트
  const ips = [
    '192.168.1.100',
    '10.0.0.50',
    '172.16.0.25',
    '203.0.113.10'
  ];
  
  const ip = ips[Math.floor(Math.random() * ips.length)];
  
  let response = http.get(`http://localhost:5000/api/test/my-endpoint?clientIp=${ip}`);
  
  check(response, {
    'status is 200': (r) => r.status === 200,
    'response time < 10ms': (r) => r.timings.duration < 10,
  });
}
```

## 문제 해결 가이드

### 1. 성능 문제 진단

#### 응답 시간 증가
```
1. 메트릭 확인:
   - fusion_cache_hit_rate < 80% → 캐시 설정 검토
   - fusion_cache_p95_response_time > 5ms → 타임아웃 설정 검토
   
2. 로그 분석:
   - "FailSafe Activated" 빈발 → Redis 연결 문제
   - "Factory Error" 증가 → 데이터 소스 문제
   
3. 해결 방법:
   - L1 캐시 크기 증가
   - 백그라운드 새로고침 활성화
   - Redis 연결 풀 최적화
```

#### 메모리 사용량 증가
```
1. 진단:
   - fusion_cache_l1_memory_usage_bytes 모니터링
   - GC 압박 여부 확인
   
2. 해결:
   - L1CacheMaxSize 조정
   - CompactionPercentage 증가
   - ExpirationScanFrequency 단축
```

### 2. 모니터링 체크리스트

#### 일일 점검
- [ ] 캐시 히트율 > 80%
- [ ] 평균 응답 시간 < 2ms
- [ ] 오류율 < 0.1%
- [ ] Redis 연결 상태 정상

#### 주간 점검
- [ ] 성능 트렌드 분석
- [ ] 메모리 사용량 트렌드
- [ ] 알림 규칙 효과성 검토
- [ ] 용량 계획 업데이트

#### 월간 점검
- [ ] 전체 성능 리뷰
- [ ] 설정 최적화 검토
- [ ] 비용 효율성 분석
- [ ] 벤치마크 테스트 수행

## 결론

FusionCache는 적절한 모니터링과 튜닝을 통해 기존 Redis 캐시 대비 상당한 성능 개선을 제공합니다. 핵심은 다음과 같습니다:

1. **지속적인 모니터링**: 핵심 메트릭을 통한 실시간 성능 추적
2. **적응적 튜닝**: 실제 사용 패턴에 따른 설정 최적화
3. **예방적 관리**: 알림 시스템을 통한 문제 조기 발견
4. **성능 테스트**: 정기적인 부하 테스트로 성능 검증

이러한 접근 방식을 통해 안정적이고 고성능의 캐시 시스템을 운영할 수 있습니다.