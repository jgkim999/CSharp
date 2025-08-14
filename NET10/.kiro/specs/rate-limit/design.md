# Rate Limiting 설계 문서

## 개요

FastEndpoints의 내장 Rate Limiting 기능을 사용하여 UserCreateEndpointV1에 IP 기반 요청 제한을 구현합니다. 이 설계는 분당 10회 요청 제한을 통해 API 남용을 방지하고 시스템 안정성을 보장합니다.

## 아키텍처

### 전체 구조

```
HTTP Request → FastEndpoints Rate Limiter → UserCreateEndpointV1 → Business Logic
                     ↓ (Rate Limit 초과시)
                429 Response
```

### 구성 요소

1. **FastEndpoints Rate Limiter**: 요청 제한 로직을 처리하는 내장 미들웨어
2. **IP 식별자**: 클라이언트를 고유하게 식별하는 메커니즘
3. **Rate Limit 정책**: 제한 규칙과 윈도우 설정
4. **응답 처리기**: Rate Limit 초과시 적절한 응답 생성

## 컴포넌트 및 인터페이스

### 1. Rate Limiting 설정

FastEndpoints의 Rate Limiting은 엔드포인트 레벨에서 구성됩니다:

```csharp
public override void Configure()
{
    Post("/api/user/create");
    AllowAnonymous();
    
    // Rate Limiting 적용
    Throttle(
        hitLimit: 10,           // 분당 10회 제한
        durationSeconds: 60,    // 60초 윈도우
        headerName: null        // 기본값: X-Forwarded-For 또는 RemoteIpAddress 사용
    );
}
```

### 2. 클라이언트 식별 메커니즘

FastEndpoints는 다음 순서로 클라이언트를 식별합니다:

1. **X-Forwarded-For 헤더**: 프록시 환경에서 실제 클라이언트 IP 확인
2. **HttpContext.Connection.RemoteIpAddress**: 직접 연결된 클라이언트 IP
3. **실패시**: 403 Forbidden 응답 반환

### 3. Rate Limit 저장소

FastEndpoints는 메모리 기반 저장소를 사용하여 요청 카운터를 관리합니다:

- **키**: 클라이언트 IP 주소
- **값**: 현재 윈도우 내 요청 횟수
- **만료**: 윈도우 기간 후 자동 리셋

## 데이터 모델

### Rate Limit 상태

```csharp
internal class RateLimitState
{
    public string ClientIdentifier { get; set; }  // IP 주소
    public int RequestCount { get; set; }         // 현재 요청 횟수
    public DateTime WindowStart { get; set; }     // 윈도우 시작 시간
    public DateTime WindowEnd { get; set; }       // 윈도우 종료 시간
}
```

### 응답 모델

Rate Limit 초과시 반환되는 응답:

```csharp
public class RateLimitResponse
{
    public int StatusCode { get; set; } = 429;
    public string Message { get; set; } = "Too many requests. Please try again later.";
    public Dictionary<string, string> Headers { get; set; } = new()
    {
        ["Retry-After"] = "60"  // 재시도 가능한 시간(초)
    };
}
```

## 에러 처리

### 1. Rate Limit 초과

**시나리오**: 클라이언트가 분당 10회를 초과하여 요청

**처리 방법**:

- HTTP 429 Too Many Requests 응답
- "Too many requests. Please try again later." 메시지
- Retry-After 헤더에 재시도 가능한 시간 포함

### 2. 클라이언트 식별 실패

**시나리오**: IP 주소를 확인할 수 없는 경우

**처리 방법**:

- HTTP 403 Forbidden 응답
- FastEndpoints에서 자동으로 처리

### 3. 시스템 오류

**시나리오**: Rate Limiting 메커니즘 자체의 오류

**처리 방법**:

- 로깅을 통한 오류 기록
- 기본적으로 요청 허용 (Fail-Open 정책)

## 테스트 전략

### 1. 단위 테스트

- **Rate Limit 적용 확인**: 10회 요청 후 429 응답 검증
- **윈도우 리셋 확인**: 60초 후 카운터 리셋 검증
- **IP 식별 로직**: 다양한 IP 주소로 독립적인 제한 확인

### 2. 통합 테스트

- **엔드포인트 통합**: UserCreateEndpointV1과 Rate Limiting 통합 테스트
- **동시 요청**: 여러 클라이언트의 동시 요청 처리 확인
- **프록시 환경**: X-Forwarded-For 헤더를 통한 IP 식별 테스트

### 3. 성능 테스트

- **부하 테스트**: Rate Limiting이 성능에 미치는 영향 측정
- **메모리 사용량**: 다수의 클라이언트 IP에 대한 메모리 사용량 확인
- **응답 시간**: Rate Limiting 적용 전후 응답 시간 비교

## 보안 고려사항

### 1. 제한사항

FastEndpoints 문서에 따른 보안 제한사항:

- **보안 목적 부적합**: DDOS 공격 방어용으로 사용하면 안됨
- **우회 가능성**: 악의적인 클라이언트가 헤더 값을 조작하여 우회 가능
- **NAT/프록시 환경**: 동일한 IP를 공유하는 환경에서 부정확할 수 있음

### 2. 권장사항

- **게이트웨이 레벨**: 더 강력한 보안을 위해 API Gateway에서 Rate Limiting 구현 고려
- **인증 기반**: 가능한 경우 사용자 인증 정보 기반 Rate Limiting 고려
- **모니터링**: Rate Limit 위반 패턴 모니터링을 통한 악의적 활동 탐지

## 설정 관리

### 1. 구성 가능한 값

```csharp
public class RateLimitConfig
{
    public int HitLimit { get; set; } = 10;        // 요청 제한 횟수
    public int DurationSeconds { get; set; } = 60; // 윈도우 기간(초)
    public string? HeaderName { get; set; }        // 클라이언트 식별 헤더
}
```

### 2. 환경별 설정

- **개발 환경**: 더 관대한 제한 (예: 분당 100회)
- **테스트 환경**: 테스트에 적합한 제한 (예: 분당 50회)
- **운영 환경**: 엄격한 제한 (예: 분당 10회)

## 모니터링 및 로깅

### 1. 로깅 전략

```csharp
// Rate Limit 적용시
_logger.LogInformation("Rate limit applied for IP: {ClientIP}, Endpoint: {Endpoint}", 
    clientIP, "/api/user/create");

// Rate Limit 초과시
_logger.LogWarning("Rate limit exceeded for IP: {ClientIP}, Endpoint: {Endpoint}, Count: {RequestCount}", 
    clientIP, "/api/user/create", requestCount);
```

### 2. 메트릭 수집

- **요청 횟수**: IP별, 엔드포인트별 요청 통계
- **Rate Limit 위반**: 위반 횟수 및 패턴
- **성능 지표**: Rate Limiting으로 인한 응답 시간 영향

## 구현 우선순위

1. **1단계**: 기본 Rate Limiting 적용
   - UserCreateEndpointV1에 Throttle 설정 추가
   - 기본 IP 기반 클라이언트 식별

2. **2단계**: 응답 개선
   - 사용자 정의 에러 메시지
   - Retry-After 헤더 추가

3. **3단계**: 모니터링 및 로깅
   - Rate Limit 관련 로그 추가
   - 메트릭 수집 구현

4. **4단계**: 테스트 및 검증
   - 단위 테스트 작성
   - 통합 테스트 구현
