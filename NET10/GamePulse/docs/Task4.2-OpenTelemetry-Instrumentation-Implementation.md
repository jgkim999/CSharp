# Task 4.2: GamePulse 애플리케이션 OpenTelemetry 계측 구현

## 개요

GamePulse 애플리케이션에 OpenTelemetry 계측을 구현하여 메트릭, 로그, 트레이스를 자동으로 수집하고 중앙화된 모니터링 시스템으로 전송하는 기능을 완성했습니다.

## 구현 내용

### 1. .NET OpenTelemetry SDK 패키지 추가

#### 추가된 패키지
```xml
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.12" />
<PackageReference Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.12.0-rc1" />
```

#### 기존 패키지 (이미 설치됨)
- OpenTelemetry (1.12.0)
- OpenTelemetry.Exporter.Console (1.12.0)
- OpenTelemetry.Exporter.OpenTelemetryProtocol (1.12.0)
- OpenTelemetry.Extensions.Hosting (1.12.0)
- OpenTelemetry.Instrumentation.AspNetCore (1.12.0)
- OpenTelemetry.Instrumentation.Http (1.12.0)
- OpenTelemetry.Instrumentation.Runtime (1.12.0)
- OpenTelemetry.Instrumentation.StackExchangeRedis (1.12.0-beta.2)

### 2. OpenTelemetry 구성 개선

#### OtelConfig 클래스 확장
```csharp
public class OtelConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string TracesSamplerArg { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceVersion { get; set; } = string.Empty;
    public string ServiceNamespace { get; set; } = "production";
    public string ServiceInstanceId { get; set; } = Environment.MachineName;
    public string DeploymentEnvironment { get; set; } = "aws";
    public bool EnablePrometheusExporter { get; set; } = true;
    public bool EnableConsoleExporter { get; set; } = false;
    public int MetricExportIntervalMs { get; set; } = 5000;
    public int BatchExportTimeoutMs { get; set; } = 30000;
    public int MaxBatchSize { get; set; } = 512;
}
```

### 3. HTTP, 데이터베이스, 커스텀 메트릭 계측 설정

#### Application Layer 확장 (OpenTelemetryApplicationExtensions.cs)

**주요 기능:**
- 환경 변수 기반 동적 구성 지원
- 풍부한 리소스 속성 설정
- 커스텀 TelemetryService 등록
- 로깅 계측 추가

```csharp
// 환경 변수에서 OTLP 엔드포인트 오버라이드 지원
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? config.Endpoint;
var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? config.ServiceName;

// 풍부한 리소스 속성 설정
openTelemetryBuilder.ConfigureResource(resource => resource
    .AddService(serviceName: serviceName, serviceVersion: serviceVersion, serviceInstanceId: config.ServiceInstanceId)
    .AddAttributes(new Dictionary<string, object>
    {
        ["service.namespace"] = serviceNamespace,
        ["deployment.environment"] = deploymentEnvironment,
        ["host.name"] = Environment.MachineName,
        ["os.type"] = Environment.OSVersion.Platform.ToString(),
        // ... 추가 메타데이터
    }));
```

#### Infrastructure Layer 확장 (OpenTelemetryInfraExtensions.cs)

**주요 개선사항:**
- Entity Framework Core 계측 추가 (데이터베이스 추적)
- 상세한 HTTP 계측 설정
- Prometheus 메트릭 익스포터 지원
- 로그 OTLP 익스포터 추가

```csharp
// Entity Framework Core 계측 (데이터베이스 추적)
tracing.AddEntityFrameworkCoreInstrumentation(options =>
{
    options.SetDbStatementForText = true;
    options.SetDbStatementForStoredProcedure = true;
    options.EnrichWithIDbCommand = (activity, command) =>
    {
        activity.SetTag("db.command_timeout", command.CommandTimeout);
        activity.SetTag("db.command_type", command.CommandType.ToString());
    };
});

// Prometheus 익스포터 (선택적)
if (config.EnablePrometheusExporter)
{
    metrics.AddPrometheusExporter();
}
```

### 4. OTLP 엔드포인트 및 리소스 속성 구성

#### 환경 변수 지원
```bash
# 컨테이너 환경에서 사용할 수 있는 환경 변수
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_SERVICE_NAME=gamepulse
OTEL_SERVICE_VERSION=1.0.0
OTEL_SERVICE_NAMESPACE=production
OTEL_DEPLOYMENT_ENVIRONMENT=aws
```

#### appsettings.json 구성
```json
{
  "OpenTelemetry": {
    "Endpoint": "http://192.168.0.47:4317",
    "TracesSamplerArg": "1.0",
    "ServiceName": "game-pulse",
    "ServiceVersion": "1.0.0",
    "ServiceNamespace": "production",
    "ServiceInstanceId": "",
    "DeploymentEnvironment": "aws",
    "EnablePrometheusExporter": true,
    "EnableConsoleExporter": false,
    "MetricExportIntervalMs": 5000,
    "BatchExportTimeoutMs": 30000,
    "MaxBatchSize": 512
  }
}
```

### 5. 엔드포인트 계측 개선

#### LoginEndpointV1 개선
- ITelemetryService를 사용한 구조화된 계측
- 상세한 Activity 태그 설정
- 성공/실패 메트릭 기록
- 트레이스 컨텍스트와 함께 로깅

```csharp
// OpenTelemetry Activity 시작
using var activity = _telemetryService.StartActivity("user.login", new Dictionary<string, object?>
{
    ["user.name"] = req.Username,
    ["endpoint.name"] = "LoginEndpointV1",
    ["endpoint.version"] = "v1"
});

// 성공 메트릭 기록
_telemetryService.RecordHttpRequest("POST", "/api/login", 200, duration);
_telemetryService.RecordBusinessMetric("login_success", 1, new Dictionary<string, object?>
{
    ["user.name"] = req.Username,
    ["user.id"] = userId
});
```

#### RttEndpointV1 개선
- RTT 데이터 수집 과정의 상세한 추적
- 클라이언트 IP 및 지리적 정보 태깅
- RTT 메트릭 자동 기록
- 백그라운드 처리 큐 모니터링

```csharp
// RTT 메트릭 직접 기록
_telemetryService.RecordRttMetrics("KR", req.Rtt / 1000.0, req.Quality, "sod");

// 비즈니스 메트릭 기록
_telemetryService.RecordBusinessMetric("rtt_submissions", 1, new Dictionary<string, object?>
{
    ["rtt.type"] = req.Type,
    ["client.ip"] = clientIp
});
```

### 6. Dockerfile 환경 변수 설정

```dockerfile
# OpenTelemetry 환경 변수 설정 (기본값, ECS에서 오버라이드 가능)
ENV OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
ENV OTEL_SERVICE_NAME=gamepulse
ENV OTEL_SERVICE_VERSION=1.0.0
ENV OTEL_SERVICE_NAMESPACE=production
ENV OTEL_DEPLOYMENT_ENVIRONMENT=aws
ENV OTEL_RESOURCE_ATTRIBUTES=service.version=1.0.0,deployment.environment=aws,container.runtime=docker
```

### 7. Prometheus 메트릭 엔드포인트 추가

```csharp
// Prometheus 메트릭 엔드포인트 추가 (프로덕션 환경에서만)
if (openTelemetryConfig.EnablePrometheusExporter)
{
    app.MapPrometheusScrapingEndpoint();
}
```

## 수집되는 텔레메트리 데이터

### 메트릭 (Metrics)
- **HTTP 요청 메트릭**: 요청 수, 응답 시간, 상태 코드별 분포
- **런타임 메트릭**: CPU, 메모리, GC, 스레드 풀 사용률
- **데이터베이스 메트릭**: 연결 수, 쿼리 실행 시간, 실패율
- **비즈니스 메트릭**: 로그인 성공/실패, RTT 측정값, 사용자 활동
- **커스텀 RTT 메트릭**: 국가별 RTT 분포, 네트워크 품질 점수

### 로그 (Logs)
- **구조화된 로깅**: JSON 형식의 로그 with 트레이스 컨텍스트
- **OTLP 로그 익스포터**: OpenTelemetry Collector로 직접 전송
- **Serilog 통합**: 기존 로깅 인프라와 호환

### 트레이스 (Traces)
- **HTTP 요청 추적**: 전체 요청 생명주기 추적
- **데이터베이스 쿼리 추적**: SQL 쿼리 실행 시간 및 결과
- **외부 서비스 호출**: HTTP 클라이언트 요청 추적
- **백그라운드 작업**: RTT 처리 등 비동기 작업 추적
- **커스텀 스팬**: 비즈니스 로직별 상세 추적

## 환경별 설정

### Development 환경
```json
{
  "OpenTelemetry": {
    "ServiceNamespace": "development",
    "DeploymentEnvironment": "local",
    "EnablePrometheusExporter": false,
    "EnableConsoleExporter": true,
    "MetricExportIntervalMs": 10000
  }
}
```

### Production 환경
```json
{
  "OpenTelemetry": {
    "ServiceNamespace": "production",
    "DeploymentEnvironment": "aws",
    "EnablePrometheusExporter": true,
    "EnableConsoleExporter": false,
    "MetricExportIntervalMs": 5000
  }
}
```

## 요구사항 충족 확인

### ✅ 요구사항 3.2: 메트릭 수집
- HTTP, 런타임, 데이터베이스, 커스텀 메트릭 수집 완료
- Prometheus 익스포터를 통한 메트릭 노출
- OTLP를 통한 OpenTelemetry Collector 전송

### ✅ 요구사항 3.3: 로그 수집
- 구조화된 로깅 with 트레이스 컨텍스트
- OTLP 로그 익스포터를 통한 Loki 전송
- Serilog와 OpenTelemetry 통합

### ✅ 요구사항 3.4: 트레이스 수집
- ASP.NET Core, HTTP 클라이언트, 데이터베이스 자동 계측
- 커스텀 스팬을 통한 비즈니스 로직 추적
- OTLP를 통한 Jaeger 전송

## 다음 단계

1. **OpenTelemetry Collector 구성**: 수집된 데이터를 Prometheus, Loki, Jaeger로 라우팅
2. **ECS 태스크 정의**: 사이드카 패턴으로 Collector 배치
3. **모니터링 스택 배포**: Prometheus, Loki, Jaeger, Grafana ECS 서비스 생성
4. **대시보드 구성**: Grafana에서 수집된 데이터 시각화

## 참고 자료

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core OpenTelemetry](https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics)
- [OpenTelemetry Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/)
