# OpenTelemetry Task 4.2: Serilog와 OpenTelemetry 통합 구현

## 개요

이 문서는 Demo.Web 프로젝트에서 Serilog와 OpenTelemetry를 통합하여 구조화된 로깅과 분산 추적을 연결하는 구현 내용을 설명합니다.

## 구현된 기능

### 1. Serilog 설정 업데이트

#### Program.cs 수정사항

- Serilog 설정을 OpenTelemetry와 통합하도록 업데이트
- 트레이스 ID와 스팬 ID를 로그에 자동 포함
- OpenTelemetry 싱크 설정 추가
- ASP.NET Core 로깅 시스템과 Serilog 통합

```csharp
// Serilog와 OpenTelemetry 통합 설정
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", otelConfig.ServiceName)
    .Enrich.WithProperty("ServiceVersion", otelConfig.ServiceVersion)
    .Enrich.WithProperty("Environment", otelConfig.Environment)
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "{NewLine}{Exception} " +
        "TraceId={TraceId} SpanId={SpanId}")
    .WriteTo.OpenTelemetry(options => { /* OTLP 설정 */ })
    .CreateLogger();

// ASP.NET Core와 Serilog 통합
builder.Host.UseSerilog();
```

### 2. 환경별 Serilog 설정

#### appsettings.json

- 기본 Serilog 설정 추가
- 콘솔 및 파일 출력 설정
- 트레이스 ID/스팬 ID 포함 출력 템플릿 설정

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.OpenTelemetry"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {NewLine}{Exception} TraceId={TraceId} SpanId={SpanId}{NewLine}"
        }
      }
    ]
  }
}
```

#### appsettings.Development.json

- 개발 환경용 상세 로깅 설정
- Debug 레벨 로깅 활성화
- SourceContext 포함 출력 템플릿

#### appsettings.Production.json

- 프로덕션 환경용 최적화된 로깅 설정
- 파일 크기 제한 및 롤링 설정
- 30일 로그 보관 정책

### 3. TelemetryService 로깅 기능 확장

#### 추가된 메서드들

1. **LogWithTraceContext**: 트레이스 컨텍스트와 함께 로그 기록
2. **LogInformationWithTrace**: 정보 로그 + 트레이스 컨텍스트
3. **LogWarningWithTrace**: 경고 로그 + 트레이스 컨텍스트
4. **LogErrorWithTrace**: 에러 로그 + 트레이스 컨텍스트
5. **CreateLogContext**: 구조화된 로깅을 위한 컨텍스트 생성

#### 사용 예시

```csharp
public class UserController : ControllerBase
{
    private readonly ILogger<UserController> _logger;
    private readonly TelemetryService _telemetryService;

    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        using var activity = _telemetryService.StartActivity("CreateUser", new Dictionary<string, object?>
        {
            ["user.email"] = request.Email,
            ["operation.type"] = "user_creation"
        });

        try
        {
            // 트레이스 컨텍스트와 함께 로그 기록
            TelemetryService.LogInformationWithTrace(_logger, 
                "사용자 생성 시작: {UserEmail}", request.Email);

            // 비즈니스 로직 실행
            var user = await CreateUserAsync(request);

            TelemetryService.LogInformationWithTrace(_logger, 
                "사용자 생성 완료: {UserId}", user.Id);

            TelemetryService.SetActivitySuccess(activity, "User created successfully");
            return Ok(user);
        }
        catch (Exception ex)
        {
            TelemetryService.LogErrorWithTrace(_logger, ex, 
                "사용자 생성 실패: {UserEmail}", request.Email);
            
            TelemetryService.SetActivityError(activity, ex);
            throw;
        }
    }
}
```

### 4. 구조화된 로깅 컨텍스트

#### CompositeDisposable 클래스

- 여러 IDisposable 객체를 관리하는 유틸리티 클래스
- 로그 컨텍스트의 안전한 해제를 보장

#### 사용 예시

```csharp
using var logContext = TelemetryService.CreateLogContext(new Dictionary<string, object>
{
    ["UserId"] = userId,
    ["Operation"] = "UserUpdate",
    ["RequestId"] = requestId
});

_logger.Information("사용자 업데이트 처리 중");
// 이 로그에는 자동으로 TraceId, SpanId, UserId, Operation, RequestId가 포함됨
```

## 로그 출력 형태

### 개발 환경

```
[14:30:25 INF] Demo.Web.Controllers.UserController 사용자 생성 시작: user@example.com 
TraceId=80f198ee56343ba864fe8b2a57d3eff7 SpanId=e457b5a2e4d86bd1
```

### 프로덕션 환경

```
[2024-02-09 14:30:25.123 +09:00 INF] 사용자 생성 시작: user@example.com 
TraceId=80f198ee56343ba864fe8b2a57d3eff7 SpanId=e457b5a2e4d86bd1
```

## 설정 옵션

### OpenTelemetry 로깅 설정

```json
{
  "OpenTelemetry": {
    "Logging": {
      "Enabled": true,
      "IncludeTraceId": true,
      "IncludeSpanId": true,
      "StructuredLogging": true
    }
  }
}
```

### Serilog OpenTelemetry 싱크 설정

- **Endpoint**: OTLP 엔드포인트 URL
- **Protocol**: OTLP 프로토콜 (grpc/http)
- **Headers**: OTLP 헤더 설정
- **ResourceAttributes**: 리소스 속성 추가

## 장점

1. **통합된 관찰성**: 로그와 트레이스가 자동으로 연결됨
2. **구조화된 로깅**: 일관된 로그 형식과 메타데이터
3. **환경별 최적화**: 개발/프로덕션 환경에 맞는 설정
4. **성능 최적화**: 비동기 로깅과 배치 처리
5. **중앙 집중식 로깅**: OTLP를 통한 중앙 로그 수집

## 모니터링 및 알림

### 로그 기반 메트릭

- 에러 로그 발생률
- 경고 로그 빈도
- 특정 패턴 로그 감지

### 알림 설정

- 에러 로그 임계값 초과 시 알림
- 트레이스 ID를 통한 문제 추적
- 로그 볼륨 급증 감지

## 문제 해결

### 일반적인 문제들

1. **트레이스 ID가 로그에 나타나지 않는 경우**
   - Activity.Current가 null인지 확인
   - OpenTelemetry 초기화 순서 확인
   - Serilog 설정에서 FromLogContext enricher 확인

2. **OTLP 싱크 연결 실패**
   - 엔드포인트 URL 확인
   - 네트워크 연결 상태 확인
   - 인증 헤더 설정 확인

3. **로그 성능 문제**
   - 비동기 싱크 사용 확인
   - 배치 크기 조정
   - 로그 레벨 최적화

4. **Serilog.Sinks.OpenTelemetry API 변경**
   - 패키지 버전에 따라 API가 변경될 수 있음
   - IncludeScopes, IncludeFormattedMessage 등의 속성이 제거됨
   - 최신 문서 참조 필요

## 다음 단계

1. **Task 5.1**: TelemetryService를 사용한 사용자 정의 계측 구현
2. **Task 6.1**: LiteBus 파이프라인에 텔레메트리 동작 추가
3. **Task 8.1**: 모니터링 대시보드 구성

## 참고 자료

- [Serilog OpenTelemetry Sink](https://github.com/serilog/serilog-sinks-opentelemetry)
- [OpenTelemetry .NET Logging](https://opentelemetry.io/docs/instrumentation/net/logging/)
- [ASP.NET Core Logging](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/)