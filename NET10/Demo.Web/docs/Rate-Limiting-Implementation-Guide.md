# Rate Limiting 구현 가이드

## 개요

이 문서는 Demo.Web 프로젝트의 UserCreateEndpointV1에 구현된 IP 기반 Rate Limiting 기능에 대한 포괄적인 가이드입니다. FastEndpoints의 내장 Rate Limiting 기능을 사용하여 API 남용을 방지하고 시스템 안정성을 보장합니다.

## 구현 내용

### 1. 기본 Rate Limiting 적용

UserCreateEndpointV1에 다음과 같은 Rate Limiting이 적용되었습니다:

- **제한 횟수**: 분당 10회
- **윈도우 기간**: 60초
- **클라이언트 식별**: IP 주소 기반 (X-Forwarded-For 또는 RemoteIpAddress)

```csharp
public override void Configure()
{
    Post("/api/user/create");
    AllowAnonymous();
    
    // Rate Limiting 적용
    Throttle(
        hitLimit: 10,           // 분당 10회 제한
        durationSeconds: 60,    // 60초 윈도우
        headerName: null        // 기본 IP 식별 사용
    );
}
```

### 2. 사용자 정의 응답

Rate Limit 초과시 다음과 같은 응답이 반환됩니다:

- **HTTP 상태 코드**: 429 Too Many Requests
- **에러 메시지**: "Too many requests. Please try again later."
- **Retry-After 헤더**: 재시도 가능한 시간(초) 포함

### 3. 설정 관리

Rate Limiting 설정은 `RateLimitConfig` 클래스를 통해 관리됩니다:

```csharp
public class RateLimitConfig
{
    public int HitLimit { get; set; } = 10;
    public int DurationSeconds { get; set; } = 60;
    public string? HeaderName { get; set; }
}
```

### 4. 로깅 및 모니터링

Rate Limiting 관련 이벤트는 다음과 같이 로깅됩니다:

- **정보 로그**: Rate Limit 적용시
- **경고 로그**: Rate Limit 초과시
- **포함 정보**: 클라이언트 IP, 엔드포인트, 요청 횟수

## 설정 방법

### 1. appsettings.json 설정

환경별로 다른 Rate Limiting 설정을 적용할 수 있습니다:

```json
{
  "RateLimit": {
    "HitLimit": 10,
    "DurationSeconds": 60,
    "HeaderName": null
  }
}
```

### 2. 환경별 설정 예시

#### 개발 환경 (appsettings.Development.json)

```json
{
  "RateLimit": {
    "HitLimit": 100,
    "DurationSeconds": 60
  }
}
```

#### 운영 환경 (appsettings.Production.json)

```json
{
  "RateLimit": {
    "HitLimit": 10,
    "DurationSeconds": 60
  }
}
```

### 3. 프로그래밍 방식 설정

필요시 코드에서 직접 설정을 변경할 수 있습니다:

```csharp
public override void Configure()
{
    Post("/api/user/create");
    AllowAnonymous();
    
    var config = Resolve<IConfiguration>();
    var rateLimitConfig = config.GetSection("RateLimit").Get<RateLimitConfig>();
    
    Throttle(
        hitLimit: rateLimitConfig.HitLimit,
        durationSeconds: rateLimitConfig.DurationSeconds,
        headerName: rateLimitConfig.HeaderName
    );
}
```

## 사용 가이드

### 1. 클라이언트 측 처리

클라이언트는 429 응답을 받았을 때 다음과 같이 처리해야 합니다:

```javascript
// JavaScript 예시
async function createUser(userData) {
    try {
        const response = await fetch('/api/user/create', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userData)
        });
        
        if (response.status === 429) {
            const retryAfter = response.headers.get('Retry-After');
            console.log(`Rate limit exceeded. Retry after ${retryAfter} seconds`);
            
            // 지정된 시간 후 재시도
            setTimeout(() => createUser(userData), retryAfter * 1000);
            return;
        }
        
        return await response.json();
    } catch (error) {
        console.error('Request failed:', error);
    }
}
```

### 2. 테스트 방법

Rate Limiting 동작을 테스트하려면:

```bash
# 연속으로 요청을 보내어 Rate Limit 테스트
for i in {1..15}; do
    curl -X POST http://localhost:5000/api/user/create \
         -H "Content-Type: application/json" \
         -d '{"name":"test","email":"test@example.com"}' \
         -w "Request $i: %{http_code}\n"
done
```

### 3. 모니터링

로그를 통해 Rate Limiting 상태를 모니터링할 수 있습니다:

```bash
# Rate Limit 관련 로그 확인
grep "Rate limit" logs/demo-web-*.log
```

## 주의사항

### 1. 기능적 제한사항

- **메모리 기반**: Rate Limit 카운터는 메모리에 저장되므로 애플리케이션 재시작시 리셋됩니다
- **단일 인스턴스**: 여러 서버 인스턴스 간에는 Rate Limit이 공유되지 않습니다
- **IP 기반 제한**: 동일한 NAT 뒤의 여러 사용자가 제한을 공유할 수 있습니다

### 2. 프록시 환경 고려사항

프록시나 로드 밸런서 뒤에서 실행될 때:

- **X-Forwarded-For 헤더**: 실제 클라이언트 IP를 전달하도록 프록시 설정 필요
- **신뢰할 수 있는 프록시**: 헤더 조작을 방지하기 위해 신뢰할 수 있는 프록시만 허용

### 3. 성능 고려사항

- **메모리 사용량**: 많은 수의 고유 IP가 접근할 경우 메모리 사용량 증가
- **응답 시간**: Rate Limiting 체크로 인한 약간의 응답 시간 증가 가능

## 보안 제한사항 및 권장사항

### 1. 보안 제한사항

FastEndpoints 문서에 명시된 중요한 제한사항들:

#### ⚠️ DDOS 공격 방어 부적합

- **제한사항**: 이 Rate Limiting은 DDOS 공격 방어용으로 설계되지 않았습니다
- **이유**: 메모리 기반 저장소와 단순한 IP 기반 식별의 한계

#### ⚠️ 헤더 조작 가능성

- **제한사항**: 악의적인 클라이언트가 X-Forwarded-For 헤더를 조작하여 제한을 우회할 수 있습니다
- **영향**: 신뢰할 수 없는 네트워크 환경에서는 보안 효과가 제한적

#### ⚠️ NAT/프록시 환경의 부정확성

- **제한사항**: 동일한 IP를 공유하는 환경에서 정확한 클라이언트 식별이 어려움
- **영향**: 정당한 사용자가 다른 사용자의 과도한 요청으로 인해 제한될 수 있음

### 2. 보안 권장사항

#### 🔒 게이트웨이 레벨 보안

```
인터넷 → API Gateway (강력한 Rate Limiting) → 애플리케이션 (기본 Rate Limiting)
```

- **권장**: AWS API Gateway, Azure API Management, Kong 등에서 1차 Rate Limiting 구현
- **이유**: 더 강력한 보안 기능과 분산 환경 지원

#### 🔒 인증 기반 Rate Limiting

```csharp
// 사용자 인증 정보 기반 Rate Limiting (권장)
public override void Configure()
{
    Post("/api/user/create");
    Policies("AuthenticatedUser");  // 인증 필요
    
    // 사용자 ID 기반 Rate Limiting
    Throttle(
        hitLimit: 10,
        durationSeconds: 60,
        headerName: "X-User-ID"  // 인증된 사용자 ID 사용
    );
}
```

#### 🔒 다층 보안 전략

1. **네트워크 레벨**: 방화벽, DDoS 보호 서비스
2. **게이트웨이 레벨**: API Gateway Rate Limiting
3. **애플리케이션 레벨**: FastEndpoints Rate Limiting (현재 구현)
4. **데이터베이스 레벨**: 연결 풀 제한, 쿼리 타임아웃

### 3. 모니터링 및 알림

#### 📊 모니터링 지표

- Rate Limit 위반 횟수 및 패턴
- 비정상적인 트래픽 증가
- 특정 IP의 반복적인 위반

#### 🚨 알림 설정

```csharp
// 예시: 비정상적인 Rate Limit 위반 감지
if (rateLimitViolations > threshold)
{
    _logger.LogCritical("Potential attack detected from IP: {ClientIP}", clientIP);
    // 알림 시스템 호출
}
```

### 4. 운영 환경 체크리스트

#### ✅ 배포 전 확인사항

- [ ] 프록시/로드 밸런서에서 X-Forwarded-For 헤더 올바르게 설정
- [ ] 환경별 Rate Limit 설정 적절히 구성
- [ ] 모니터링 및 로깅 시스템 준비
- [ ] 클라이언트 측 429 응답 처리 로직 구현

#### ✅ 운영 중 모니터링

- [ ] Rate Limit 위반 패턴 정기 검토
- [ ] 정당한 사용자의 불편 사항 모니터링
- [ ] 시스템 리소스 사용량 추적
- [ ] 보안 이벤트 로그 분석

## 문제 해결

### 1. 일반적인 문제

#### 문제: 정당한 사용자가 차단됨

**원인**: NAT 환경에서 여러 사용자가 동일한 IP 공유
**해결책**:

- Rate Limit 임계값 조정
- 인증 기반 Rate Limiting 고려
- 화이트리스트 기능 추가

#### 문제: Rate Limit이 작동하지 않음

**원인**: 프록시 환경에서 실제 IP 식별 실패
**해결책**:

- X-Forwarded-For 헤더 설정 확인
- 프록시 설정 검토
- 로그를 통한 IP 식별 과정 확인

### 2. 디버깅 방법

#### 로그 레벨 조정

```json
{
  "Logging": {
    "LogLevel": {
      "FastEndpoints": "Debug"
    }
  }
}
```

#### 상세 로깅 활성화

```csharp
_logger.LogDebug("Client IP identified as: {ClientIP} from headers: {Headers}", 
    clientIP, string.Join(", ", headers));
```

## 결론

이 Rate Limiting 구현은 기본적인 API 보호 기능을 제공하지만, 완전한 보안 솔루션은 아닙니다. 운영 환경에서는 다층 보안 전략의 일부로 사용하고, 지속적인 모니터링과 개선이 필요합니다.

더 강력한 보안이 필요한 경우, API Gateway 레벨의 Rate Limiting과 인증 기반 제한을 함께 고려하시기 바랍니다.