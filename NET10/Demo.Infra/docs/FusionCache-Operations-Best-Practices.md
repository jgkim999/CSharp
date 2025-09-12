# FusionCache 운영 가이드 및 베스트 프랙티스

## 개요

이 문서는 FusionCache를 프로덕션 환경에서 안전하고 효율적으로 운영하기 위한 베스트 프랙티스와 운영 가이드를 제공합니다.

## 운영 베스트 프랙티스

### 1. 설정 관리

#### 환경별 설정 분리
```json
// appsettings.json (기본 설정)
{
  "FusionCache": {
    "DefaultEntryOptions": "00:30:00",
    "EnableFailSafe": true,
    "EnableEagerRefresh": true
  }
}

// appsettings.Production.json (프로덕션 오버라이드)
{
  "FusionCache": {
    "L1CacheMaxSize": 2000,
    "EnableDetailedLogging": false,
    "CacheEventLogLevel": "Warning"
  }
}

// appsettings.Development.json (개발 오버라이드)
{
  "FusionCache": {
    "L1CacheMaxSize": 100,
    "EnableDetailedLogging": true,
    "CacheEventLogLevel": "Debug"
  }
}
```

#### 설정 유효성 검증
```csharp
// Startup.cs 또는 Program.cs에서
services.PostConfigure<FusionCacheConfig>(config =>
{
    var (isValid, errors) = config.Validate();
    if (!isValid)
    {
        throw new InvalidOperationException(
            $"FusionCache 설정이 유효하지 않습니다: {string.Join(", ", errors)}");
    }
});
```

### 2. 모니터링 및 알림

#### 핵심 메트릭 모니터링
```yaml
# prometheus.yml
rule_files:
  - "fusioncache_rules.yml"

# fusioncache_rules.yml
groups:
  - name: fusioncache_sla
    interval: 30s
    rules:
      # SLA 메트릭 정의
      - record: fusioncache:hit_rate_5m
        expr: rate(fusion_cache_hits_total[5m]) / (rate(fusion_cache_hits_total[5m]) + rate(fusion_cache_misses_total[5m]))
        
      - record: fusioncache:error_rate_5m
        expr: rate(fusion_cache_errors_total[5m]) / rate(fusion_cache_operations_total[5m])
        
      - record: fusioncache:p99_latency_5m
        expr: histogram_quantile(0.99, rate(fusion_cache_operation_duration_seconds_bucket[5m]))
```

#### 알림 계층화
```yaml
# 알림 심각도별 분류
groups:
  - name: fusioncache_critical
    rules:
      - alert: FusionCacheDown
        expr: up{job="fusioncache"} == 0
        for: 1m
        labels:
          severity: critical
          team: platform
        annotations:
          summary: "FusionCache 서비스가 다운되었습니다"
          runbook_url: "https://wiki.company.com/fusioncache-runbook"
          
  - name: fusioncache_warning
    rules:
      - alert: FusionCacheHighLatency
        expr: fusioncache:p99_latency_5m > 0.01
        for: 5m
        labels:
          severity: warning
          team: platform
        annotations:
          summary: "FusionCache 지연시간이 증가했습니다"
```

### 3. 로깅 전략

#### 구조화된 로깅
```csharp
// IpToNationFusionCache.cs에서
_logger.LogInformation("캐시 조회 완료: {Operation} {ClientIpHash} {Result} {Duration}ms",
    "GetAsync",
    HashIpForLogging(clientIp),
    result.IsSuccess ? "Hit" : "Miss",
    stopwatch.ElapsedMilliseconds);
```

#### 로그 레벨 관리
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Demo.Infra.Repositories.IpToNationFusionCache": "Information",
      "ZiggyCreatures.Caching.Fusion": "Warning"
    }
  }
}
```

### 4. 보안 고려사항

#### 민감한 정보 보호
```csharp
// IP 주소 해싱 (개인정보 보호)
private static string HashIpForLogging(string clientIp)
{
    using var sha256 = SHA256.Create();
    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(clientIp));
    return Convert.ToHexString(hashBytes)[..8];
}

// 설정 정보 마스킹
public override string ToString()
{
    var maskedConnectionString = string.IsNullOrEmpty(ConnectionString) 
        ? "Not configured" 
        : $"{ConnectionString[..Math.Min(10, ConnectionString.Length)]}***";
    
    return $"FusionCache Config - ConnectionString: {maskedConnectionString}";
}
```

#### Redis 보안 설정
```json
{
  "Redis": {
    "IpToNationConnectionString": "localhost:6379,ssl=true,password=${REDIS_PASSWORD}",
    "KeyPrefix": "${ENVIRONMENT}"
  }
}
```

## 용량 계획

### 1. 메모리 사용량 계산

#### 예상 메모리 사용량
```
단일 캐시 항목 크기:
- 키: "dev:ipcache:192.168.1.100" ≈ 30 bytes
- 값: "KR" ≈ 2 bytes  
- 오버헤드: ≈ 100 bytes
- 총합: ≈ 132 bytes

환경별 예상 사용량:
- 개발 (100개): 13.2 KB
- 스테이징 (1,000개): 132 KB
- 프로덕션 (10,000개): 1.32 MB
```

#### 메모리 모니터링
```csharp
// FusionCacheMetricsService.cs에서
_meter.CreateObservableGauge("fusion_cache_memory_usage_bytes", () =>
{
    // L1 캐시 메모리 사용량 추정
    return _fusionCache.GetCurrentMemoryUsage();
});
```

### 2. 네트워크 대역폭

#### Redis 트래픽 계산
```
요청당 네트워크 사용량:
- GET 요청: ~50 bytes
- SET 요청: ~80 bytes
- 응답: ~30 bytes

1,000 RPS 기준:
- 인바운드: 80 KB/s
- 아웃바운드: 30 KB/s
- 총합: 110 KB/s ≈ 0.88 Mbps
```

### 3. 확장성 계획

#### 수평 확장
```yaml
# Kubernetes 배포 예시
apiVersion: apps/v1
kind: Deployment
metadata:
  name: demo-web
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: demo-web
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        env:
        - name: FusionCache__L1CacheMaxSize
          value: "1000"
```

## 장애 대응 가이드

### 1. 일반적인 장애 시나리오

#### Redis 연결 실패
```
증상:
- "FusionCache L2 (Redis) 캐시를 사용할 수 없습니다" 로그
- fusion_cache_redis_connection_status = 0

진단:
1. Redis 서버 상태 확인
2. 네트워크 연결 테스트
3. 인증 정보 확인

대응:
1. 페일세이프 동작 확인 (L1 캐시만으로 서비스 지속)
2. Redis 서버 복구
3. 연결 복구 후 정상 동작 확인
```

#### 메모리 부족
```
증상:
- OutOfMemoryException 발생
- GC 압박 증가
- 응답 시간 증가

진단:
1. fusion_cache_l1_memory_usage_bytes 확인
2. 애플리케이션 전체 메모리 사용량 분석
3. GC 로그 분석

대응:
1. L1CacheMaxSize 임시 감소
2. CompactionPercentage 증가
3. 애플리케이션 재시작 (필요시)
```

#### 성능 저하
```
증상:
- 응답 시간 증가
- 처리량 감소
- 캐시 히트율 저하

진단:
1. 캐시 히트율 분석
2. L1/L2 캐시 분포 확인
3. 백그라운드 새로고침 동작 확인

대응:
1. 캐시 설정 튜닝
2. 백그라운드 새로고침 활성화
3. 캐시 워밍업 수행
```

### 2. 장애 대응 체크리스트

#### 즉시 대응 (5분 이내)
- [ ] 서비스 상태 확인
- [ ] 핵심 메트릭 점검
- [ ] 최근 변경사항 확인
- [ ] 긴급 롤백 필요성 판단

#### 단기 대응 (30분 이내)
- [ ] 상세 로그 분석
- [ ] 근본 원인 파악
- [ ] 임시 해결책 적용
- [ ] 모니터링 강화

#### 장기 대응 (24시간 이내)
- [ ] 근본 원인 해결
- [ ] 재발 방지 대책 수립
- [ ] 문서 업데이트
- [ ] 사후 검토 수행

## 성능 최적화

### 1. 캐시 워밍업

#### 애플리케이션 시작 시 워밍업
```csharp
public class CacheWarmupService : IHostedService
{
    private readonly IIpToNationCache _cache;
    private readonly ILogger<CacheWarmupService> _logger;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("캐시 워밍업을 시작합니다");
        
        // 자주 사용되는 IP 범위 미리 로드
        var commonIps = new[]
        {
            "8.8.8.8",      // Google DNS
            "1.1.1.1",      // Cloudflare DNS
            "208.67.222.222" // OpenDNS
        };

        foreach (var ip in commonIps)
        {
            try
            {
                await _cache.GetAsync(ip);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "캐시 워밍업 중 오류 발생: {Ip}", ip);
            }
        }
        
        _logger.LogInformation("캐시 워밍업이 완료되었습니다");
    }
}
```

### 2. 동적 설정 조정

#### 런타임 설정 변경
```csharp
public class DynamicFusionCacheConfigService : BackgroundService
{
    private readonly IOptionsMonitor<FusionCacheConfig> _configMonitor;
    private readonly IFusionCache _fusionCache;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _configMonitor.OnChange(config =>
        {
            _logger.LogInformation("FusionCache 설정이 변경되었습니다: {Config}", config);
            
            // 동적 설정 적용
            ApplyDynamicConfiguration(config);
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private void ApplyDynamicConfiguration(FusionCacheConfig config)
    {
        // L1 캐시 크기 조정 (재시작 필요)
        if (config.L1CacheMaxSize != _currentConfig.L1CacheMaxSize)
        {
            _logger.LogWarning("L1 캐시 크기 변경은 애플리케이션 재시작이 필요합니다");
        }

        // 로깅 레벨 동적 변경
        UpdateLoggingLevel(config.CacheEventLogLevel);
    }
}
```

## 백업 및 복구

### 1. 설정 백업

#### 설정 파일 버전 관리
```bash
# Git을 통한 설정 관리
git add appsettings*.json
git commit -m "FusionCache 설정 업데이트: L1 캐시 크기 증가"
git tag -a "config-v1.2.0" -m "FusionCache 설정 v1.2.0"
```

#### 설정 검증 스크립트
```bash
#!/bin/bash
# validate-config.sh

echo "FusionCache 설정 검증 중..."

# JSON 문법 검증
for config in appsettings*.json; do
    if ! jq empty "$config" 2>/dev/null; then
        echo "ERROR: $config 파일의 JSON 문법이 잘못되었습니다"
        exit 1
    fi
done

# 필수 설정 확인
required_keys=("FusionCache.DefaultEntryOptions" "FusionCache.L1CacheMaxSize")
for key in "${required_keys[@]}"; do
    if ! jq -e ".$key" appsettings.json >/dev/null; then
        echo "ERROR: 필수 설정 $key가 없습니다"
        exit 1
    fi
done

echo "설정 검증 완료"
```

### 2. 데이터 백업

#### Redis 데이터 백업
```bash
#!/bin/bash
# backup-redis.sh

REDIS_HOST="localhost"
REDIS_PORT="6379"
BACKUP_DIR="/backup/redis"
DATE=$(date +%Y%m%d_%H%M%S)

echo "Redis 데이터 백업 시작: $DATE"

# RDB 백업
redis-cli -h $REDIS_HOST -p $REDIS_PORT BGSAVE

# 백업 파일 복사
cp /var/lib/redis/dump.rdb "$BACKUP_DIR/dump_$DATE.rdb"

# 압축
gzip "$BACKUP_DIR/dump_$DATE.rdb"

echo "Redis 데이터 백업 완료: $BACKUP_DIR/dump_$DATE.rdb.gz"
```

## 테스트 전략

### 1. 성능 테스트

#### 부하 테스트 자동화
```yaml
# .github/workflows/performance-test.yml
name: Performance Test
on:
  schedule:
    - cron: '0 2 * * *'  # 매일 새벽 2시

jobs:
  performance-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup k6
        run: |
          sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
          echo "deb https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
          sudo apt-get update
          sudo apt-get install k6
          
      - name: Run performance test
        run: k6 run --out json=results.json performance-test.js
        
      - name: Analyze results
        run: |
          # 성능 기준 검증
          AVG_RESPONSE_TIME=$(jq '.metrics.http_req_duration.avg' results.json)
          if (( $(echo "$AVG_RESPONSE_TIME > 10" | bc -l) )); then
            echo "성능 기준 미달: 평균 응답시간 ${AVG_RESPONSE_TIME}ms"
            exit 1
          fi
```

### 2. 카나리 배포

#### 점진적 배포 전략
```yaml
# kubernetes/canary-deployment.yml
apiVersion: argoproj.io/v1alpha1
kind: Rollout
metadata:
  name: demo-web
spec:
  replicas: 10
  strategy:
    canary:
      steps:
      - setWeight: 10    # 10% 트래픽
      - pause: {duration: 5m}
      - setWeight: 25    # 25% 트래픽
      - pause: {duration: 10m}
      - setWeight: 50    # 50% 트래픽
      - pause: {duration: 10m}
      analysis:
        templates:
        - templateName: success-rate
        args:
        - name: service-name
          value: demo-web
```

## 문서화 및 지식 공유

### 1. 운영 문서

#### Runbook 작성
```markdown
# FusionCache 장애 대응 Runbook

## 1. Redis 연결 실패
### 증상
- 로그: "FusionCache L2 (Redis) 캐시를 사용할 수 없습니다"
- 메트릭: fusion_cache_redis_connection_status = 0

### 대응 절차
1. Redis 서버 상태 확인: `redis-cli ping`
2. 네트워크 연결 테스트: `telnet redis-host 6379`
3. 페일세이프 동작 확인: L1 캐시만으로 서비스 지속되는지 확인
4. Redis 서버 재시작 (필요시)

### 에스컬레이션
- 30분 내 해결되지 않으면 인프라팀에 에스컬레이션
- 연락처: infrastructure-team@company.com
```

### 2. 지식 공유

#### 정기 리뷰 미팅
```
월간 FusionCache 운영 리뷰:
- 성능 메트릭 분석
- 장애 사례 공유
- 최적화 기회 식별
- 베스트 프랙티스 업데이트
```

#### 교육 자료
```markdown
# FusionCache 교육 자료

## 기본 개념
- L1/L2 캐시 구조
- 페일세이프 메커니즘
- 백그라운드 새로고침

## 실습
- 로컬 환경 설정
- 모니터링 대시보드 사용법
- 장애 시뮬레이션 및 대응
```

## 결론

FusionCache의 성공적인 운영을 위해서는:

1. **체계적인 모니터링**: 핵심 메트릭과 알림 시스템 구축
2. **예방적 관리**: 정기적인 성능 테스트와 용량 계획
3. **신속한 대응**: 명확한 장애 대응 절차와 에스컬레이션 경로
4. **지속적인 개선**: 운영 경험을 바탕으로 한 최적화와 문서 업데이트

이러한 베스트 프랙티스를 따르면 안정적이고 고성능의 캐시 시스템을 운영할 수 있습니다.