# Rate Limiting 설정 클래스 구현 문서

## 개요

Rate Limiting 설정을 위한 구성 클래스를 생성하고 appsettings.json에서 설정값을 읽어오는 기능을 구현했습니다.

## 구현 내용

### 1. RateLimitConfig 클래스 생성

`NET10/Demo.Web/Configs/RateLimitConfig.cs` 파일을 생성하여 Rate Limiting 설정을 관리하는 클래스를 구현했습니다.

#### 주요 구성 요소

- **RateLimitConfig**: 메인 설정 클래스
- **UserCreateEndpointConfig**: 사용자 생성 엔드포인트별 설정
- **GlobalRateLimitConfig**: 전역 Rate Limiting 설정

#### 설정 가능한 값들

```csharp
public class UserCreateEndpointConfig
{
    public bool Enabled { get; set; } = true;           // Rate Limiting 활성화 여부
    public int HitLimit { get; set; } = 10;             // 요청 제한 횟수
    public int DurationSeconds { get; set; } = 60;      // 윈도우 기간 (초)
    public string? HeaderName { get; set; }             // 클라이언트 식별 헤더
    public string ErrorMessage { get; set; }            // 에러 메시지
    public int RetryAfterSeconds { get; set; } = 60;    // Retry-After 헤더 값
}
```

### 2. 환경별 설정 구성

#### appsettings.json (기본 설정)

- HitLimit: 10회/분
- DurationSeconds: 60초
- 모든 로깅 활성화

#### appsettings.Development.json (개발 환경)

- HitLimit: 100회/분 (더 관대한 설정)
- 상세한 로깅 활성화

#### appsettings.Production.json (운영 환경)

- HitLimit: 5회/분 (더 엄격한 설정)
- Rate Limit 적용 로그는 비활성화, 초과 로그만 활성화

### 3. DI 컨테이너 등록

Program.cs에서 설정을 바인딩하고 DI 컨테이너에 등록:

```csharp
// RateLimit 설정 바인딩
var rateLimitConfig = new RateLimitConfig();
builder.Configuration.GetSection(RateLimitConfig.SectionName).Bind(rateLimitConfig);

// DI 컨테이너에 등록
builder.Services.Configure<RateLimitConfig>(builder.Configuration.GetSection(RateLimitConfig.SectionName));
builder.Services.AddSingleton(rateLimitConfig);
```

### 4. UserCreateEndpointV1 수정

엔드포인트에서 설정 클래스를 주입받아 사용하도록 수정:

```csharp
public UserCreateEndpointV1(
    ICommandMediator commandMediator,
    ITelemetryService telemetryService,
    ILogger<UserCreateEndpointV1> logger,
    RateLimitConfig rateLimitConfig)
{
    // 의존성 주입
}

public override void Configure()
{
    Post("/api/user/create");
    AllowAnonymous();

    // 설정 파일에서 읽어온 값 사용
    if (_rateLimitConfig.UserCreateEndpoint.Enabled)
    {
        Throttle(
            hitLimit: _rateLimitConfig.UserCreateEndpoint.HitLimit,
            durationSeconds: _rateLimitConfig.UserCreateEndpoint.DurationSeconds,
            headerName: _rateLimitConfig.UserCreateEndpoint.HeaderName
        );
    }
}
```

## 설정 예시

### 기본 설정 (appsettings.json)

```json
{
  "RateLimit": {
    "UserCreateEndpoint": {
      "Enabled": true,
      "HitLimit": 10,
      "DurationSeconds": 60,
      "HeaderName": null,
      "ErrorMessage": "Too many requests. Please try again later.",
      "RetryAfterSeconds": 60
    },
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

### 개발 환경 설정 (appsettings.Development.json)

```json
{
  "RateLimit": {
    "UserCreateEndpoint": {
      "HitLimit": 100
    }
  }
}
```

### 운영 환경 설정 (appsettings.Production.json)

```json
{
  "RateLimit": {
    "UserCreateEndpoint": {
      "HitLimit": 5
    },
    "Global": {
      "LogRateLimitApplied": false
    }
  }
}
```

## 장점

1. **환경별 설정**: 개발, 테스트, 운영 환경에 맞는 다른 Rate Limit 설정 적용 가능
2. **런타임 설정 변경**: appsettings.json 파일 수정으로 애플리케이션 재시작 없이 설정 변경 가능
3. **타입 안전성**: 강타입 설정 클래스로 컴파일 타임 검증
4. **확장성**: 새로운 엔드포인트나 설정 추가 시 쉽게 확장 가능
5. **중앙 집중식 관리**: 모든 Rate Limiting 설정을 한 곳에서 관리

## 요구사항 충족

- ✅ RateLimitConfig 클래스 생성하여 설정값 관리
- ✅ appsettings.json에서 설정값 읽어오는 기능 구현
- ✅ 환경별 다른 설정값 적용 가능하도록 구성
- ✅ 요구사항 3.2 충족: Rate Limiting 설정을 애플리케이션 재시작 없이 변경 가능

## 다음 단계

이제 다음 작업들을 진행할 수 있습니다:

- Rate Limiting 관련 로깅 구현 (작업 4)
- 단위 테스트 작성 (작업 5)
- 통합 테스트 작성 (작업 6)