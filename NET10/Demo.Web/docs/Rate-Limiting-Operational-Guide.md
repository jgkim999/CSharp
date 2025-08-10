# Rate Limiting 운영 가이드

## 개요

이 문서는 Rate Limiting 기능의 운영 환경 배포, 모니터링, 유지보수에 대한 실무 가이드입니다.

## 배포 가이드

### 1. 환경별 설정

#### 개발 환경
```json
{
  "RateLimit": {
    "HitLimit": 100,
    "DurationSeconds": 60,
    "HeaderName": null
  },
  "Logging": {
    "LogLevel": {
      "Demo.Web.Middleware.RateLimitMiddleware": "Debug"
    }
  }
}
```

#### 스테이징 환경
```json
{
  "RateLimit": {
    "HitLimit": 50,
    "DurationSeconds": 60,
    "HeaderName": null
  },
  "Logging": {
    "LogLevel": {
      "Demo.Web.Middleware.RateLimitMiddleware": "Information"
    }
  }
}
```

#### 운영 환경
```json
{
  "RateLimit": {
    "HitLimit": 10,
    "DurationSeconds": 60,
    "HeaderName": null
  },
  "Logging": {
    "LogLevel": {
      "Demo.Web.Middleware.RateLimitMiddleware": "Warning"
    }
  }
}
```

### 2. 프록시/로드 밸런서 설정

#### Nginx 설정 예시
```nginx
server {
    listen 80;
    server_name your-api.com;
    
    location / {
        proxy_pass http://backend;
        
        # 실제 클라이언트 IP 전달
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header Host $host;
    }
}
```

#### AWS Application Load Balancer
- **X-Forwarded-For 헤더**: 자동으로 추가됨
- **추가 설정**: 필요 없음

#### Azure Application Gateway
```json
{
  "requestRoutingRules": [{
    "httpListeners": [{
      "protocol": "Http",
      "requireServerNameIndication": false
    }],
    "backendAddressPools": [{
      "backendAddresses": [{"ipAddress": "your-backend-ip"}]
    }],
    "backendHttpSettingsCollection": [{
      "protocol": "Http",
      "port": 80,
      "requestTimeout": 30,
      "pickHostNameFromBackendAddress": false
    }]
  }]
}
```

## 모니터링 및 알림

### 1. 핵심 메트릭

#### 애플리케이션 메트릭
- **rate_limit_violations_total**: Rate Limit 위반 총 횟수
- **rate_limit_requests_total**: Rate Limit 체크된 총 요청 수
- **rate_limit_blocked_requests_total**: 차단된 요청 수

#### 시스템 메트릭
- **memory_usage**: Rate Limit 저장소 메모리 사용량
- **response_time**: Rate Limit 체크로 인한 응답 시간 증가
- **error_rate**: 429 응답 비율

### 2. 로그 모니터링

#### 중요 로그 패턴
```bash
# Rate Limit 위반 패턴 검색
grep "Rate limit exceeded" /var/log/demo-web/*.log | \
awk '{print $NF}' | sort | uniq -c | sort -nr

# 특정 IP의 위반 횟수
grep "Rate limit exceeded.*IP: 192.168.1.100" /var/log/demo-web/*.log | wc -l

# 시간대별 위반 분포
grep "Rate limit exceeded" /var/log/demo-web/*.log | \
awk '{print $1" "$2}' | cut -c1-13 | uniq -c
```

#### ELK Stack 쿼리 예시
```json
{
  "query": {
    "bool": {
      "must": [
        {"match": {"message": "Rate limit exceeded"}},
        {"range": {"@timestamp": {"gte": "now-1h"}}}
      ]
    }
  },
  "aggs": {
    "by_ip": {
      "terms": {"field": "client_ip.keyword", "size": 10}
    }
  }
}
```

### 3. 알림 설정

#### Prometheus + Grafana 알림
```yaml
# prometheus.yml
rule_files:
  - "rate_limit_rules.yml"

# rate_limit_rules.yml
groups:
- name: rate_limit_alerts
  rules:
  - alert: HighRateLimitViolations
    expr: rate(rate_limit_violations_total[5m]) > 10
    for: 2m
    labels:
      severity: warning
    annotations:
      summary: "High rate limit violations detected"
      description: "Rate limit violations: {{ $value }} per second"
      
  - alert: SuspiciousActivity
    expr: rate(rate_limit_violations_total{client_ip="specific_ip"}[1m]) > 5
    for: 1m
    labels:
      severity: critical
    annotations:
      summary: "Suspicious activity from {{ $labels.client_ip }}"
```

#### Azure Monitor 알림
```json
{
  "name": "RateLimitViolationAlert",
  "description": "Alert when rate limit violations exceed threshold",
  "severity": 2,
  "enabled": true,
  "condition": {
    "allOf": [{
      "metricName": "rate_limit_violations_total",
      "operator": "GreaterThan",
      "threshold": 100,
      "timeAggregation": "Total",
      "dimensions": []
    }]
  },
  "actions": [{
    "actionGroupId": "/subscriptions/{subscription-id}/resourceGroups/{rg}/providers/microsoft.insights/actionGroups/{action-group}"
  }]
}
```

## 성능 최적화

### 1. 메모리 관리

#### 메모리 사용량 모니터링
```csharp
// 메모리 사용량 추적을 위한 커스텀 미들웨어
public class RateLimitMemoryMonitor
{
    private readonly ILogger<RateLimitMemoryMonitor> _logger;
    private readonly Timer _timer;
    
    public RateLimitMemoryMonitor(ILogger<RateLimitMemoryMonitor> logger)
    {
        _logger = logger;
        _timer = new Timer(LogMemoryUsage, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }
    
    private void LogMemoryUsage(object state)
    {
        var memoryUsage = GC.GetTotalMemory(false);
        _logger.LogInformation("Rate limit memory usage: {MemoryUsage} bytes", memoryUsage);
    }
}
```

#### 메모리 정리 전략
```csharp
// 주기적인 메모리 정리 (필요시)
public class RateLimitCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 만료된 Rate Limit 엔트리 정리
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            GC.Collect(); // 필요시에만 사용
        }
    }
}
```

### 2. 응답 시간 최적화

#### 비동기 처리
```csharp
// Rate Limit 체크를 비동기로 처리 (FastEndpoints에서 자동 처리됨)
public override async Task HandleAsync(UserCreateRequest req, CancellationToken ct)
{
    // Rate Limit은 FastEndpoints에서 자동으로 처리
    // 비즈니스 로직에 집중
    var result = await _userService.CreateUserAsync(req, ct);
    await SendOkAsync(result, ct);
}
```

## 보안 강화

### 1. IP 화이트리스트

```csharp
public class TrustedIpRateLimitPolicy
{
    private readonly HashSet<string> _trustedIps = new()
    {
        "192.168.1.0/24",  // 내부 네트워크
        "10.0.0.0/8",      // 사내 네트워크
        "203.0.113.0/24"   // 파트너 네트워크
    };
    
    public bool IsTrustedIp(string clientIp)
    {
        // IP 범위 체크 로직
        return _trustedIps.Any(range => IsIpInRange(clientIp, range));
    }
}
```

### 2. 동적 Rate Limit 조정

```csharp
public class AdaptiveRateLimitService
{
    public int CalculateRateLimit(string clientIp, DateTime currentTime)
    {
        // 시간대별 조정
        var hour = currentTime.Hour;
        var baseLimit = 10;
        
        // 업무 시간 (9-18시)에는 더 관대한 제한
        if (hour >= 9 && hour <= 18)
        {
            return baseLimit * 2;
        }
        
        // 심야 시간에는 더 엄격한 제한
        if (hour >= 23 || hour <= 6)
        {
            return baseLimit / 2;
        }
        
        return baseLimit;
    }
}
```

## 문제 해결 가이드

### 1. 일반적인 문제와 해결책

#### 문제: 429 응답이 너무 많이 발생
**진단**:
```bash
# 429 응답 비율 확인
grep "429" /var/log/nginx/access.log | wc -l
grep "200\|201" /var/log/nginx/access.log | wc -l
```

**해결책**:
1. Rate Limit 임계값 조정
2. 클라이언트 측 재시도 로직 개선
3. 캐싱 전략 도입

#### 문제: 특정 IP에서 지속적인 위반
**진단**:
```bash
# 특정 IP의 요청 패턴 분석
grep "192.168.1.100" /var/log/demo-web/*.log | \
awk '{print $1" "$2}' | sort | uniq -c
```

**해결책**:
1. IP 차단 (방화벽 레벨)
2. 더 엄격한 Rate Limit 적용
3. 보안팀에 보고

### 2. 성능 문제 해결

#### 메모리 누수 의심시
```bash
# 메모리 사용량 모니터링
ps aux | grep dotnet
top -p $(pgrep dotnet)

# GC 정보 확인
dotnet-counters monitor --process-id $(pgrep dotnet) \
  --counters System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count]
```

#### 응답 시간 증가시
```bash
# 응답 시간 분석
tail -f /var/log/nginx/access.log | \
awk '{print $NF}' | sort -n | tail -10
```

## 유지보수 체크리스트

### 일일 점검
- [ ] Rate Limit 위반 로그 검토
- [ ] 시스템 리소스 사용량 확인
- [ ] 비정상적인 트래픽 패턴 확인

### 주간 점검
- [ ] Rate Limit 설정 적정성 검토
- [ ] 성능 메트릭 분석
- [ ] 보안 이벤트 로그 분석

### 월간 점검
- [ ] Rate Limit 정책 전반 검토
- [ ] 용량 계획 수립
- [ ] 보안 위협 분석 및 대응 방안 수립

## 업그레이드 가이드

### 1. FastEndpoints 업그레이드시 주의사항
- Rate Limiting API 변경사항 확인
- 기존 설정 호환성 검증
- 테스트 환경에서 충분한 검증 후 적용

### 2. .NET 업그레이드시 고려사항
- 메모리 관리 방식 변경 확인
- 성능 특성 변화 모니터링
- 로깅 프레임워크 호환성 확인

이 운영 가이드를 통해 Rate Limiting 기능을 안정적으로 운영하고 지속적으로 개선할 수 있습니다.