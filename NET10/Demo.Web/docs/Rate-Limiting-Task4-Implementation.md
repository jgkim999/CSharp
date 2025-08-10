# Rate Limiting 로깅 구현 문서

## 개요

이 문서는 Demo.Web 프로젝트의 Rate Limiting 기능에 로깅 기능을 추가한 구현 내용을 설명합니다.

## 구현된 기능

### 1. Rate Limit 적용시 정보 로그 기록

- **위치**: `RateLimitMiddleware.LogRequestInfo()` 메서드
- **로그 레벨**: Information
- **포함 정보**:
  - 클라이언트 IP 주소
  - 요청 엔드포인트
  - 현재 요청 횟수 / 최대 허용 횟수

```csharp
_logger.LogInformation("Rate limit applied for IP: {ClientIP}, Endpoint: {Endpoint}, RequestCount: {RequestCount}/{HitLimit}", 
    clientIp, endpoint, requestInfo.RequestCount, _rateLimitConfig.UserCreateEndpoint.HitLimit);
```

### 2. Rate Limit 초과시 경고 로그 기록

- **위치**: `RateLimitMiddleware.HandleRateLimitResponse()` 메서드
- **로그 레벨**: Warning
- **포함 정보**:
  - 클라이언트 IP 주소
  - 요청 엔드포인트
  - 현재 요청 횟수

```csharp
_logger.LogWarning("Rate limit exceeded for IP: {ClientIP}, Endpoint: {Endpoint}, RequestCount: {RequestCount}", 
    clientIp, endpoint, requestCount);
```

### 3. 클라이언트 IP 식별 로직

- **우선순위**:
  1. `X-Forwarded-For` 헤더 (프록시 환경 대응)
  2. `HttpContext.Connection.RemoteIpAddress`
- **다중 IP 처리**: X-Forwarded-For에 여러 IP가 있는 경우 첫 번째 IP 사용

```csharp
private string GetClientIpAddress(HttpContext context)
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    
    if (_rateLimitConfig.Global.IncludeClientIpInLogs)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            clientIp = forwardedFor.Split(',')[0].Trim();
        }
    }
    
    return clientIp;
}
```

### 4. 요청 카운터 추적

- **저장소**: 메모리 기반 `ConcurrentDictionary<string, ClientRequestInfo>`
- **키**: 클라이언트 IP 주소
- **값**: 요청 횟수, 윈도우 시작 시간, 마지막 요청 시간
- **윈도우 관리**: 60초 윈도우 만료시 자동 리셋

```csharp
private class ClientRequestInfo
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime LastRequestTime { get; set; }
}
```

## 설정 옵션

### appsettings.json 설정

```json
{
  "RateLimit": {
    "Global": {
      "EnableLogging": true,
      "LogRateLimitApplied": true,
      "LogRateLimitExceeded": true,
      "IncludeClientIpInLogs": true,
      "IncludeRequestCountInLogs": true
    }
  }
}
```

### 설정 옵션 설명

- **EnableLogging**: 전체 Rate Limiting 로깅 활성화/비활성화
- **LogRateLimitApplied**: Rate Limit 적용시 정보 로그 기록 여부
- **LogRateLimitExceeded**: Rate Limit 초과시 경고 로그 기록 여부
- **IncludeClientIpInLogs**: 로그에 클라이언트 IP 정보 포함 여부
- **IncludeRequestCountInLogs**: 로그에 요청 횟수 정보 포함 여부

## 로그 출력 예시

### 정상 요청시 (Information 레벨)

```
[14:30:15 INF] Rate limit applied for IP: 192.168.1.100, Endpoint: /api/user/create, RequestCount: 1/10
[14:30:16 INF] Rate limit applied for IP: 192.168.1.100, Endpoint: /api/user/create, RequestCount: 2/10
[14:30:17 INF] Rate limit applied for IP: 192.168.1.100, Endpoint: /api/user/create, RequestCount: 3/10
```

### Rate Limit 초과시 (Warning 레벨)

```
[14:30:25 WRN] Rate limit exceeded for IP: 192.168.1.100, Endpoint: /api/user/create, RequestCount: 11
[14:30:26 WRN] Rate limit exceeded for IP: 192.168.1.100, Endpoint: /api/user/create, RequestCount: 12
```

## 성능 고려사항

### 메모리 사용량

- 클라이언트별 요청 정보를 메모리에 저장
- 윈도우 만료시 자동 정리되지 않으므로 장기간 실행시 메모리 누수 가능성
- 향후 개선: 백그라운드 정리 작업 또는 LRU 캐시 구현 고려

### 동시성 처리

- `ConcurrentDictionary` 사용으로 스레드 안전성 보장
- 높은 동시 요청에서도 안정적인 카운터 업데이트

## 보안 고려사항

### IP 스푸핑 방지

- X-Forwarded-For 헤더는 클라이언트가 조작 가능
- 신뢰할 수 있는 프록시에서만 해당 헤더 사용 권장
- 보안이 중요한 환경에서는 네트워크 레벨 Rate Limiting 병행 사용

### 로그 정보 보호

- 클라이언트 IP 정보가 로그에 기록됨
- GDPR 등 개인정보 보호 규정 준수 필요
- 필요시 IP 마스킹 또는 해싱 고려

## 테스트 방법

### 수동 테스트

제공된 `test-rate-limit.sh` 스크립트 사용:

```bash
chmod +x test-rate-limit.sh
./test-rate-limit.sh
```

### 로그 확인

개발 환경에서 로그 파일 확인:

```bash
tail -f logs/demo-web-dev-*.log | grep -E "(Rate limit|IP:)"
```

## 향후 개선사항

1. **메모리 관리**: 만료된 클라이언트 정보 자동 정리
2. **분산 환경 지원**: Redis 등 외부 저장소 사용
3. **메트릭 수집**: Prometheus 메트릭으로 Rate Limiting 통계 수집
4. **알림 기능**: 과도한 Rate Limit 위반시 알림 발송
5. **IP 화이트리스트**: 특정 IP에 대한 Rate Limiting 예외 처리

## 관련 파일

- `NET10/Demo.Web/Middleware/RateLimitMiddleware.cs`: 메인 구현
- `NET10/Demo.Web/Configs/RateLimitConfig.cs`: 설정 클래스
- `NET10/Demo.Web/appsettings.json`: 운영 환경 설정
- `NET10/Demo.Web/appsettings.Development.json`: 개발 환경 설정
- `NET10/Demo.Web/test-rate-limit.sh`: 테스트 스크립트

## 요구사항 충족 확인

✅ **요구사항 3.1**: Rate Limit 적용시 정보 로그 기록  
✅ **요구사항 3.1**: Rate Limit 초과시 경고 로그 기록  
✅ **요구사항 3.1**: 클라이언트 IP와 요청 횟수 정보 포함  

모든 요구사항이 성공적으로 구현되었습니다.
