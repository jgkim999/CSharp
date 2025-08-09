# OpenTelemetry 문제 해결 가이드

## 개요

이 문서는 Demo.Web 프로젝트에서 OpenTelemetry 사용 중 발생할 수 있는 일반적인 문제들과 해결 방법을 제공합니다.

## 목차

1. [성능 관련 문제](#성능-관련-문제)
2. [데이터 수집 문제](#데이터-수집-문제)
3. [설정 관련 문제](#설정-관련-문제)
4. [네트워크 및 연결 문제](#네트워크-및-연결-문제)
5. [메모리 및 리소스 문제](#메모리-및-리소스-문제)
6. [로깅 통합 문제](#로깅-통합-문제)
7. [진단 도구 및 명령어](#진단-도구-및-명령어)

## 성능 관련 문제

### 1. 높은 CPU 사용률

**증상**:

- OpenTelemetry 도입 후 CPU 사용률이 10% 이상 증가
- 애플리케이션 응답 시간 저하

**원인**:

- 샘플링 비율이 너무 높음 (100% 샘플링)
- 동기식 익스포트 사용
- 너무 많은 스팬 생성
- 압축 비활성화

**해결책**:

```json
{
  "OpenTelemetry": {
    "Tracing": {
      "SamplingRatio": 0.1,  // 10%로 감소
      "FilterHealthChecks": true,
      "FilterStaticFiles": true,
      "MinimumDurationMs": 10  // 짧은 스팬 필터링
    },
    "Performance": {
      "UseAsyncExport": true,
      "EnableCompression": true,
      "EnableGCOptimization": true
    }
  }
}
```

**검증 방법**:

```bash
# CPU 사용률 모니터링
top -p $(pgrep -f "Demo.Web")

# 성능 카운터 확인
dotnet-counters monitor --process-id <PID> --counters System.Runtime
```

### 2. 높은 메모리 사용량

**증상**:

- 메모리 사용량이 지속적으로 증가
- OutOfMemoryException 발생
- GC 압박 증가

**원인**:

- 배치 크기가 너무 큼
- 메트릭 수집 간격이 너무 짧음
- 스팬 큐가 가득 참

**해결책**:

```json
{
  "OpenTelemetry": {
    "Metrics": {
      "MaxBatchSize": 256,
      "CollectionIntervalSeconds": 60,
      "MaxMetricStreams": 500
    },
    "ResourceLimits": {
      "MaxMemoryUsageMB": 512,
      "MaxActiveSpans": 5000,
      "MaxQueuedSpans": 25000,
      "SpanProcessorBatchSize": 1024
    }
  }
}
```

**메모리 모니터링 코드**:

```csharp
public class MemoryMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024; // MB
            if (memoryUsage > 500) // 500MB 초과 시
            {
                _logger.LogWarning("High memory usage detected: {MemoryUsage}MB", memoryUsage);
                GC.Collect();
            }
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

## 데이터 수집 문제

### 1. 트레이스 데이터 누락

**증상**:

- 일부 HTTP 요청의 트레이스가 수집되지 않음
- 스팬이 불완전하게 기록됨

**원인**:

- 샘플링으로 인한 드롭
- Activity가 제대로 시작되지 않음
- 익스포터 연결 실패

**해결책**:

```csharp
// 중요한 작업은 항상 샘플링하도록 설정
public class CriticalOperationSampler : Sampler
{
    private readonly Sampler _baseSampler;
    
    public CriticalOperationSampler(Sampler baseSampler)
    {
        _baseSampler = baseSampler;
    }
    
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        var operationName = Activity.Current?.OperationName;
        
        // 중요한 작업은 항상 샘플링
        if (operationName?.Contains("critical") == true || 
            operationName?.Contains("payment") == true ||
            operationName?.Contains("auth") == true)
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }
        
        return _baseSampler.ShouldSample(samplingParameters);
    }
}
```

**Activity 시작 확인**:

```csharp
public class ActivityDiagnosticService
{
    public static void EnsureActivityStarted(string operationName)
    {
        if (Activity.Current == null)
        {
            var activity = TelemetryService.ActivitySource.StartActivity(operationName);
            if (activity == null)
            {
                // ActivitySource가 등록되지 않았거나 샘플링으로 드롭됨
                throw new InvalidOperationException($"Failed to start activity: {operationName}");
            }
        }
    }
}
```

### 2. 메트릭 데이터 누락

**증상**:

- 사용자 정의 메트릭이 수집되지 않음
- 메트릭 값이 0으로 표시됨

**원인**:

- Meter가 제대로 등록되지 않음
- 메트릭 이름 충돌
- 익스포터 설정 오류

**해결책**:

```csharp
// Meter 등록 확인
public static class MeterRegistrationValidator
{
    public static void ValidateMeterRegistration()
    {
        var meterName = "Demo.Application";
        var meter = new Meter(meterName);
        var counter = meter.CreateCounter<long>("test_counter");
        
        // 테스트 메트릭 생성
        counter.Add(1, new TagList { { "test", "validation" } });
        
        // MeterProvider에 등록되었는지 확인
        var isRegistered = MeterProvider.Default != null;
        if (!isRegistered)
        {
            throw new InvalidOperationException($"Meter '{meterName}' is not registered");
        }
    }
}
```

## 설정 관련 문제

### 1. 환경별 설정 적용 안됨

**증상**:

- 개발 환경에서 프로덕션 설정이 적용됨
- 환경 변수가 무시됨

**원인**:

- appsettings 파일 로드 순서 문제
- 환경 변수 이름 오타
- 설정 바인딩 실패

**해결책**:

```csharp
// 설정 로드 순서 확인
public static void ValidateConfiguration(IConfiguration configuration)
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var serviceName = configuration["OpenTelemetry:ServiceName"];
    var samplingRatio = configuration.GetValue<double>("OpenTelemetry:Tracing:SamplingRatio");
    
    Console.WriteLine($"Environment: {environment}");
    Console.WriteLine($"Service Name: {serviceName}");
    Console.WriteLine($"Sampling Ratio: {samplingRatio}");
    
    // 환경별 설정 검증
    if (environment == "Development" && samplingRatio < 1.0)
    {
        throw new InvalidOperationException("Development environment should use 100% sampling");
    }
}
```

**환경 변수 오버라이드 확인**:

```bash
# 환경 변수 설정 확인
export OTEL_SERVICE_NAME="Demo.Web"
export OTEL_SERVICE_VERSION="1.0.0"
export ASPNETCORE_ENVIRONMENT="Development"

# 설정 값 확인
dotnet run --environment Development
```

### 2. OTLP 엔드포인트 연결 실패

**증상**:

- 트레이스/메트릭이 외부 시스템에 전송되지 않음
- 연결 타임아웃 오류

**원인**:

- 잘못된 엔드포인트 URL
- 네트워크 방화벽 차단
- 인증 정보 누락

**해결책**:

```csharp
// 연결 테스트 헬스체크
public class OtlpEndpointHealthCheck : IHealthCheck
{
    private readonly OpenTelemetryConfig _config;
    private readonly HttpClient _httpClient;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _config.Exporter.OtlpEndpoint;
            if (string.IsNullOrEmpty(endpoint))
            {
                return HealthCheckResult.Unhealthy("OTLP endpoint not configured");
            }
            
            // gRPC 엔드포인트 테스트
            if (_config.Exporter.OtlpProtocol.ToLower() == "grpc")
            {
                // gRPC 연결 테스트 로직
                return await TestGrpcConnection(endpoint, cancellationToken);
            }
            
            // HTTP 엔드포인트 테스트
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return response.IsSuccessStatusCode 
                ? HealthCheckResult.Healthy($"OTLP endpoint reachable: {endpoint}")
                : HealthCheckResult.Degraded($"OTLP endpoint returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"OTLP endpoint unreachable: {ex.Message}");
        }
    }
}
```

## 네트워크 및 연결 문제

### 1. 익스포터 재시도 실패

**증상**:

- 네트워크 일시 장애 시 데이터 손실
- 익스포터 오류 로그 반복

**원인**:

- 재시도 정책 미설정
- 백오프 전략 부적절
- 타임아웃 설정 부족

**해결책**:

```json
{
  "OpenTelemetry": {
    "Exporter": {
      "RetryPolicy": {
        "Enabled": true,
        "MaxRetryAttempts": 3,
        "InitialBackoffMs": 1000,
        "MaxBackoffMs": 30000,
        "BackoffMultiplier": 2.0
      },
      "TimeoutMilliseconds": 30000
    }
  }
}
```

```csharp
// 커스텀 재시도 로직
public class ResilientExporter : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        var attempt = 0;
        var backoffMs = _config.InitialBackoffMs;
        
        while (attempt <= _config.MaxRetryAttempts)
        {
            try
            {
                return _innerExporter.Export(batch);
            }
            catch (HttpRequestException ex) when (IsRetryableError(ex))
            {
                if (attempt >= _config.MaxRetryAttempts)
                {
                    _logger.LogError(ex, "Export failed after {Attempts} attempts", attempt + 1);
                    return ExportResult.Failure;
                }
                
                await Task.Delay(backoffMs);
                backoffMs = Math.Min(backoffMs * 2, _config.MaxBackoffMs);
                attempt++;
            }
        }
        
        return ExportResult.Failure;
    }
    
    private static bool IsRetryableError(Exception ex)
    {
        return ex is HttpRequestException ||
               ex is TaskCanceledException ||
               ex is SocketException;
    }
}
```

## 메모리 및 리소스 문제

### 1. 메모리 누수

**증상**:

- 장시간 실행 후 메모리 사용량 지속 증가
- GC가 메모리를 회수하지 못함

**원인**:

- Activity나 Meter의 Dispose 누락
- 이벤트 핸들러 해제 안됨
- 큰 객체가 스팬에 저장됨

**해결책**:

```csharp
// 리소스 관리 패턴
public class TelemetryService : IDisposable
{
    private bool _disposed = false;
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            ActivitySource?.Dispose();
            Meter?.Dispose();
            _disposed = true;
        }
    }
}

// 메모리 사용량 모니터링
public class MemoryPressureMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);
            var totalMemory = GC.GetTotalMemory(false);
            
            _logger.LogInformation(
                "GC Stats - Gen0: {Gen0}, Gen1: {Gen1}, Gen2: {Gen2}, Memory: {Memory:N0} bytes",
                gen0, gen1, gen2, totalMemory);
            
            if (totalMemory > 500 * 1024 * 1024) // 500MB 초과
            {
                _logger.LogWarning("High memory usage detected, forcing GC");
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

## 로깅 통합 문제

### 1. TraceId가 로그에 표시되지 않음

**증상**:

- Serilog 로그에 TraceId와 SpanId가 빈 값으로 표시
- 로그와 트레이스 연관성 부족

**원인**:

- OpenTelemetry Enricher 미설정
- Activity가 시작되지 않음
- 로그 템플릿 오류

**해결책**:

```csharp
// Serilog 설정 확인
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithOpenTelemetry()  // 이 라인이 중요!
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
        "TraceId={TraceId} SpanId={SpanId}{NewLine}")
    .CreateLogger();

// Activity 상태 확인
public static class ActivityValidator
{
    public static void LogCurrentActivity(ILogger logger)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            logger.LogInformation(
                "Current Activity - Name: {Name}, TraceId: {TraceId}, SpanId: {SpanId}",
                activity.OperationName, activity.TraceId, activity.SpanId);
        }
        else
        {
            logger.LogWarning("No current Activity found");
        }
    }
}
```

## 진단 도구 및 명령어

### 1. OpenTelemetry 상태 확인

```bash
# 환경 변수 확인
env | grep OTEL

# 프로세스 메트릭 모니터링
dotnet-counters monitor --process-id <PID> --counters System.Runtime,Microsoft.AspNetCore.Hosting

# 메모리 덤프 생성
dotnet-dump collect --process-id <PID>

# GC 정보 확인
dotnet-gcdump collect --process-id <PID>
```

### 2. 로그 분석

```bash
# 에러 로그 필터링
grep -i "error\|exception\|fail" logs/demo-web-*.log

# TraceId로 로그 추적
grep "TraceId=<trace-id>" logs/demo-web-*.log

# 성능 관련 로그
grep -i "timeout\|slow\|performance" logs/demo-web-*.log
```

### 3. 네트워크 연결 테스트

```bash
# OTLP gRPC 엔드포인트 테스트
grpcurl -plaintext localhost:4317 list

# HTTP 엔드포인트 테스트
curl -X POST http://localhost:4318/v1/traces \
  -H "Content-Type: application/x-protobuf" \
  --data-binary @test-trace.pb

# 포트 연결 확인
telnet localhost 4317
nc -zv localhost 4317
```

### 4. 성능 프로파일링

```csharp
// 커스텀 성능 카운터
public class OpenTelemetryPerformanceCounters
{
    private readonly Counter<long> _activitiesCreated;
    private readonly Counter<long> _activitiesDropped;
    private readonly Histogram<double> _exportDuration;
    
    public void RecordActivityCreated() => _activitiesCreated.Add(1);
    public void RecordActivityDropped() => _activitiesDropped.Add(1);
    public void RecordExportDuration(double duration) => _exportDuration.Record(duration);
}
```

## 자주 묻는 질문 (FAQ)

### Q1: 샘플링 비율을 어떻게 설정해야 하나요?

**A**: 환경별로 다르게 설정하는 것을 권장합니다:

- **개발**: 100% (모든 트레이스 수집)
- **스테이징**: 50% (충분한 데이터 + 성능 고려)
- **프로덕션**: 1-10% (성능 우선, 중요한 트레이스는 별도 처리)

### Q2: 메모리 사용량이 계속 증가합니다.

**A**: 다음 설정을 확인하세요:

1. `MaxActiveSpans` 제한 설정
2. `SpanProcessorBatchSize` 적절히 조정
3. `MaxMemoryUsageMB` 제한 설정
4. 정기적인 GC 수행

### Q3: 트레이스가 일부만 수집됩니다.

**A**: 샘플링 설정을 확인하고, 중요한 작업은 강제 샘플링하도록 설정하세요:

```csharp
// 중요한 작업은 항상 샘플링
if (operationName.Contains("payment") || operationName.Contains("auth"))
{
    return new SamplingResult(SamplingDecision.RecordAndSample);
}
```

### Q4: OTLP 익스포터가 연결되지 않습니다.

**A**: 다음을 확인하세요:

1. 엔드포인트 URL 정확성
2. 네트워크 연결 상태
3. 방화벽 설정
4. 인증 정보 (필요한 경우)

이 가이드를 통해 대부분의 OpenTelemetry 관련 문제를 해결할 수 있습니다. 추가적인 문제가 발생하면 로그를 자세히 분석하고 필요시 OpenTelemetry 커뮤니티에 문의하시기 바랍니다.
