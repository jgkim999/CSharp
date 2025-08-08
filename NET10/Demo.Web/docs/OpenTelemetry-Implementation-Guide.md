# Demo.Web OpenTelemetry 도입 가이드

## 개요

이 문서는 Demo.Web 프로젝트에 OpenTelemetry를 도입하는 전체 절차와 구현 방법을 설명합니다. GamePulse 프로젝트의 성공적인 OpenTelemetry 구현을 참고하여 Demo.Web에 최적화된 관찰 가능성 솔루션을 제공합니다.

## 목차

1. [사전 준비사항](#사전-준비사항)
2. [패키지 설치](#패키지-설치)
3. [설정 구성](#설정-구성)
4. [OpenTelemetry 초기화](#opentelemetry-초기화)
5. [사용자 정의 계측](#사용자-정의-계측)
6. [환경별 설정](#환경별-설정)
7. [모니터링 도구 연동](#모니터링-도구-연동)
8. [테스트 및 검증](#테스트-및-검증)
9. [성능 최적화](#성능-최적화)
10. [문제 해결](#문제-해결)

## 사전 준비사항

### 현재 Demo.Web 프로젝트 구성

- **.NET 9.0** 기반 웹 애플리케이션
- **FastEndpoints** 사용한 API 엔드포인트
- **LiteBus** 기반 CQRS 패턴
- **Serilog** 로깅 시스템
- **Scalar** API 문서화

### 필요한 도구

- Visual Studio 2022 또는 JetBrains Rider
- Docker Desktop (로컬 모니터링 도구 실행용)
- Jaeger 또는 Zipkin (트레이스 시각화)
- Prometheus + Grafana (메트릭 모니터링)

## 패키지 설치

### 1단계: 필수 OpenTelemetry 패키지 추가

```xml
<PackageReference Include="OpenTelemetry" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.12.0" />
<PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.12.0" />
```

### 2단계: Serilog OpenTelemetry 통합

```xml
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0" />
```

### 3단계: 추가 계측 패키지 (선택사항)

```xml
<!-- Entity Framework 사용 시 -->
<PackageReference Include="OpenTelemetry.Instrumentation.EntityFrameworkCore" Version="1.0.0-beta.12" />

<!-- Redis 사용 시 -->
<PackageReference Include="OpenTelemetry.Instrumentation.StackExchangeRedis" Version="1.12.0-beta.2" />
```

## 설정 구성

### 1단계: OpenTelemetry 설정 클래스 생성

```csharp
// Configs/OpenTelemetryConfig.cs
namespace Demo.Web.Configs;

public class OpenTelemetryConfig
{
    public const string SectionName = "OpenTelemetry";
    
    public string ServiceName { get; set; } = "demo-web";
    public string ServiceVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "development";
    public string Endpoint { get; set; } = "http://localhost:4317";
    public string TracesSamplerArg { get; set; } = "1.0";
    public bool EnableConsoleExporter { get; set; } = true;
    public bool EnableOtlpExporter { get; set; } = false;
}
```

### 2단계: appsettings.json 설정 추가

```json
{
  "OpenTelemetry": {
    "ServiceName": "demo-web",
    "ServiceVersion": "1.0.0",
    "Environment": "development",
    "Endpoint": "http://localhost:4317",
    "TracesSamplerArg": "1.0",
    "EnableConsoleExporter": true,
    "EnableOtlpExporter": false
  }
}
```

### 3단계: 환경별 설정

**appsettings.Development.json**

```json
{
  "OpenTelemetry": {
    "TracesSamplerArg": "1.0",
    "EnableConsoleExporter": true,
    "EnableOtlpExporter": false
  }
}
```

**appsettings.Production.json**

```json
{
  "OpenTelemetry": {
    "Environment": "production",
    "TracesSamplerArg": "0.1",
    "EnableConsoleExporter": false,
    "EnableOtlpExporter": true,
    "Endpoint": "https://your-otel-collector:4317"
  }
}
```

## OpenTelemetry 초기화

### 1단계: OpenTelemetry 초기화 클래스 생성

```csharp
// Extensions/OpenTelemetryExtensions.cs
using Demo.Web.Configs;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Demo.Web.Extensions;

public static class OpenTelemetryExtensions
{
    private static readonly ActivitySource ActivitySource = new("Demo.Web");
    
    public static IServiceCollection AddOpenTelemetryServices(
        this IServiceCollection services, 
        OpenTelemetryConfig config)
    {
        var openTelemetryBuilder = services.AddOpenTelemetry();
        
        // 샘플링 비율 설정
        if (!double.TryParse(config.TracesSamplerArg, out var probability))
            probability = 1.0;

        // 리소스 설정
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: config.ServiceName, 
                serviceVersion: config.ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["environment"] = config.Environment,
                ["host.name"] = Environment.MachineName
            });

        // 트레이싱 설정
        openTelemetryBuilder.WithTracing(tracing =>
        {
            tracing.SetResourceBuilder(resourceBuilder);
            tracing.SetSampler(new TraceIdRatioBasedSampler(probability));
            
            // 자동 계측
            tracing.AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = httpContext =>
                {
                    // Health check 엔드포인트 제외
                    return !httpContext.Request.Path.StartsWithSegments("/health");
                };
            });
            
            tracing.AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            });
            
            // 사용자 정의 소스
            tracing.AddSource("Demo.Web");
            tracing.AddSource("LiteBus");
            
            // 익스포터 설정
            if (config.EnableConsoleExporter)
                tracing.AddConsoleExporter();
                
            if (config.EnableOtlpExporter)
            {
                tracing.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(config.Endpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });
            }
        });

        // 메트릭 설정
        openTelemetryBuilder.WithMetrics(metrics =>
        {
            metrics.SetResourceBuilder(resourceBuilder);
            
            // 자동 계측
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();
            metrics.AddRuntimeInstrumentation();
            
            // .NET 8+ 기본 메트릭
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            metrics.AddMeter("System.Net.Http");
            metrics.AddMeter("System.Net.NameResolution");
            
            // 사용자 정의 메트릭
            metrics.AddMeter("Demo.Web");
            
            // 익스포터 설정
            if (config.EnableConsoleExporter)
                metrics.AddConsoleExporter();
                
            if (config.EnableOtlpExporter)
            {
                metrics.AddOtlpExporter((options, readerOptions) =>
                {
                    options.Endpoint = new Uri(config.Endpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 5000;
                });
            }
        });

        // ActivitySource 등록
        services.AddSingleton(ActivitySource);
        
        return services;
    }
}
```

### 2단계: Program.cs 수정

```csharp
using Demo.Web.Configs;
using Demo.Web.Extensions;
// ... 기존 using 문들

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry 설정 바인딩
var otelConfig = builder.Configuration
    .GetSection(OpenTelemetryConfig.SectionName)
    .Get<OpenTelemetryConfig>() ?? new OpenTelemetryConfig();

// Serilog 설정 (OpenTelemetry 통합)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otelConfig.Endpoint;
        options.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField;
    })
    .CreateLogger();

try
{
    // 기존 서비스 등록
    builder.Services.AddFastEndpoints();
    builder.Services.AddOpenApi();
    builder.Services.AddValidatorsFromAssemblyContaining<UserCreateRequestRequestValidator>();
    builder.Services.AddApplication();
    builder.Services.AddInfra(builder.Configuration);
    
    // OpenTelemetry 서비스 추가
    builder.Services.AddOpenTelemetryServices(otelConfig);
    
    var app = builder.Build();
    
    // 기존 미들웨어 설정
    app.UseFastEndpoints(x =>
    {
        x.Errors.UseProblemDetails();
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

## 사용자 정의 계측

### 1단계: 사용자 정의 ActivitySource 활용

```csharp
// Services/TelemetryService.cs
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Demo.Web.Services;

public class TelemetryService
{
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly Counter<int> _requestCounter;
    private readonly Histogram<double> _requestDuration;

    public TelemetryService(ActivitySource activitySource)
    {
        _activitySource = activitySource;
        _meter = new Meter("Demo.Web");
        
        _requestCounter = _meter.CreateCounter<int>(
            "demo_web_requests_total",
            description: "Total number of requests");
            
        _requestDuration = _meter.CreateHistogram<double>(
            "demo_web_request_duration_seconds",
            description: "Request duration in seconds");
    }

    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(name, kind);
    }

    public void RecordRequest(string endpoint, double duration, string status)
    {
        _requestCounter.Add(1, 
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status", status));
            
        _requestDuration.Record(duration,
            new KeyValuePair<string, object?>("endpoint", endpoint));
    }
}
```

### 2단계: FastEndpoints에서 사용자 정의 계측

```csharp
// Endpoints/User/UserCreateEndpoint.cs
using Demo.Web.Services;
using System.Diagnostics;

namespace Demo.Web.Endpoints.User;

public class UserCreateEndpoint : Endpoint<UserCreateRequest, UserCreateResponse>
{
    private readonly TelemetryService _telemetry;

    public UserCreateEndpoint(TelemetryService telemetry)
    {
        _telemetry = telemetry;
    }

    public override void Configure()
    {
        Post("/users");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UserCreateRequest req, CancellationToken ct)
    {
        using var activity = _telemetry.StartActivity("user.create");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            activity?.SetTag("user.email", req.Email);
            activity?.SetTag("user.name", req.Name);
            
            // 비즈니스 로직 실행
            var result = await ProcessUserCreation(req, ct);
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            await SendOkAsync(result, ct);
            
            _telemetry.RecordRequest("POST /users", stopwatch.Elapsed.TotalSeconds, "success");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            
            _telemetry.RecordRequest("POST /users", stopwatch.Elapsed.TotalSeconds, "error");
            throw;
        }
    }
}
```

### 3단계: LiteBus 명령/쿼리 계측

```csharp
// Behaviors/TelemetryBehavior.cs
using LiteBus;
using System.Diagnostics;

namespace Demo.Web.Behaviors;

public class TelemetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ActivitySource _activitySource;

    public TelemetryBehavior(ActivitySource activitySource)
    {
        _activitySource = activitySource;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        using var activity = _activitySource.StartActivity($"litebus.{requestName.ToLower()}");
        
        try
        {
            activity?.SetTag("request.type", requestName);
            activity?.SetTag("request.assembly", typeof(TRequest).Assembly.GetName().Name);
            
            var response = await next();
            
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

## 환경별 설정

### Docker Compose 개발 환경

```yaml
# docker-compose.dev.yml
version: '3.8'
services:
  demo-web:
    build: .
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - OpenTelemetry__Endpoint=http://jaeger:14268/api/traces
    depends_on:
      - jaeger

  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"
      - "14268:14268"
    environment:
      - COLLECTOR_OTLP_ENABLED=true

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
```

## 모니터링 도구 연동

### Jaeger 트레이스 확인

1. 브라우저에서 `http://localhost:16686` 접속
2. Service 드롭다운에서 "demo-web" 선택
3. "Find Traces" 클릭하여 트레이스 확인

### Prometheus 메트릭 확인

1. 브라우저에서 `http://localhost:9090` 접속
2. 쿼리 입력: `demo_web_requests_total`
3. Execute 클릭하여 메트릭 확인

### Grafana 대시보드 구성

1. 브라우저에서 `http://localhost:3000` 접속 (admin/admin)
2. Prometheus 데이터 소스 추가: `http://prometheus:9090`
3. 대시보드 생성 및 패널 구성

## 테스트 및 검증

### 1단계: 기본 기능 테스트

```bash
# 애플리케이션 시작
dotnet run

# API 호출 테스트
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com"}'
```

### 2단계: 텔레메트리 데이터 확인

- 콘솔에서 트레이스 및 메트릭 출력 확인
- Jaeger UI에서 트레이스 시각화 확인
- Prometheus에서 메트릭 수집 확인

### 3단계: 성능 테스트

```bash
# Apache Bench를 사용한 부하 테스트
ab -n 1000 -c 10 http://localhost:5000/users
```

## 성능 최적화

### 샘플링 전략

```csharp
// 프로덕션 환경에서 적응형 샘플링
tracing.SetSampler(new TraceIdRatioBasedSampler(0.1)); // 10% 샘플링
```

### 메트릭 최적화

```csharp
// 배치 익스포터 설정
metrics.AddOtlpExporter((options, readerOptions) =>
{
    readerOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 10000;
    readerOptions.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = 5000;
});
```

## 문제 해결

### 일반적인 문제들

1. **트레이스가 생성되지 않음**
   - ActivitySource 등록 확인
   - 샘플링 비율 확인

2. **메트릭이 수집되지 않음**
   - Meter 등록 확인
   - 익스포터 설정 확인

3. **성능 저하**
   - 샘플링 비율 조정
   - 불필요한 계측 제거

### 로그 확인

```csharp
// OpenTelemetry 내부 로그 활성화
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});
```

## 다음 단계

1. **고급 계측**: 데이터베이스, 캐시, 메시지 큐 계측 추가
2. **알림 설정**: 임계값 기반 알림 구성
3. **대시보드 고도화**: 비즈니스 메트릭 대시보드 구성
4. **분산 추적 확장**: 마이크로서비스 간 추적 연결

## 참고 자료

- [OpenTelemetry .NET 공식 문서](https://opentelemetry.io/docs/instrumentation/net/)
- [ASP.NET Core OpenTelemetry 가이드](https://learn.microsoft.com/en-us/aspnet/core/log-mon/metrics/metrics)
- [GamePulse OpenTelemetry 구현 참고](../GamePulse/OpenTelemetryInitialize.cs)
