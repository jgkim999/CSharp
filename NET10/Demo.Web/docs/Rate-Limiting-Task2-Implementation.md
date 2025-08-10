# Rate Limit 초과시 사용자 정의 응답 구현

## 개요

UserCreateEndpointV1에서 Rate Limit 초과시 사용자 친화적인 응답을 제공하는 기능을 구현했습니다.

## 구현된 기능

### 1. RateLimitResponse DTO 클래스

**파일**: `NET10/Demo.Application/DTO/RateLimitResponse.cs`

Rate Limit 초과시 반환되는 응답 모델을 정의했습니다:

```csharp
public class RateLimitResponse
{
    public int StatusCode { get; set; } = 429;
    public string Message { get; set; } = "Too many requests. Please try again later.";
    public string ErrorCode { get; set; } = "RATE_LIMIT_EXCEEDED";
    public int RetryAfterSeconds { get; set; } = 60;
    public string? Details { get; set; }
}
```

### 2. RateLimitMiddleware

**파일**: `NET10/Demo.Web/Middleware/RateLimitMiddleware.cs`

FastEndpoints의 Rate Limiting 기능에서 발생하는 429 응답을 가로채서 사용자 정의 응답으로 변환하는 미들웨어를 구현했습니다.

**주요 기능**:

- 429 상태 코드 응답 감지 및 가로채기
- 사용자 친화적인 한국어 에러 메시지 제공
- Retry-After 헤더 포함
- 추가 Rate Limit 정보 헤더 제공 (X-RateLimit-Limit, X-RateLimit-Window)
- 클라이언트 IP 기반 로깅

### 3. Program.cs 수정

미들웨어를 애플리케이션 파이프라인에 등록했습니다:

```csharp
// Rate Limit 미들웨어 등록 (FastEndpoints보다 먼저 등록)
app.UseMiddleware<RateLimitMiddleware>();
```

## 응답 형식

### Rate Limit 초과시 응답

**HTTP 상태 코드**: 429 Too Many Requests

**헤더**:

- `Retry-After: 60`
- `X-RateLimit-Limit: 10`
- `X-RateLimit-Window: 60`
- `Content-Type: application/json`

**응답 본문**:

```json
{
  "statusCode": 429,
  "message": "요청이 너무 많습니다. 잠시 후 다시 시도해 주세요.",
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "retryAfterSeconds": 60,
  "details": "분당 최대 10회 요청이 허용됩니다. 60초 후에 다시 시도해 주세요."
}
```

## 로깅

Rate Limit 초과시 다음과 같은 경고 로그가 기록됩니다:

```
Rate limit exceeded for IP: {ClientIP}, Endpoint: {Endpoint}
```

## 클라이언트 IP 식별

다음 순서로 클라이언트 IP를 식별합니다:

1. `X-Forwarded-For` 헤더 (프록시 환경)
2. `HttpContext.Connection.RemoteIpAddress` (직접 연결)
3. "Unknown" (식별 실패시)

## 성능 최적화

- JSON 직렬화 옵션을 정적 필드로 캐시하여 성능 최적화
- 메모리 스트림을 사용한 효율적인 응답 처리
- 비동기 처리로 스레드 블로킹 방지

## 요구사항 충족

✅ **요구사항 1.3**: 429 상태 코드와 함께 적절한 에러 메시지 반환  
✅ **요구사항 4.1**: 429 HTTP 상태 코드 반환  
✅ **요구사항 4.2**: 사용자 친화적인 에러 메시지 제공  
✅ **요구사항 4.3**: Retry-After 헤더를 포함하여 재시도 가능한 시간 정보 제공

## 테스트 방법

1. 애플리케이션 실행
2. `/api/user/create` 엔드포인트에 1분 내 10회 이상 요청
3. 11번째 요청부터 사용자 정의 429 응답 확인

## 향후 개선사항

- 설정 파일을 통한 Rate Limit 값 동적 구성
- 다양한 엔드포인트별 Rate Limit 설정 지원
- 메트릭 수집을 통한 Rate Limit 통계 제공