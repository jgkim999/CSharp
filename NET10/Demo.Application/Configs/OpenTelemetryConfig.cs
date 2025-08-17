namespace Demo.Application.Configs;

/// <summary>
/// OpenTelemetry 구성 설정 클래스
/// </summary>
public class OpenTelemetryConfig
{
    /// <summary>
    /// 구성 섹션 이름
    /// </summary>
    public const string SectionName = "OpenTelemetry";

    /// <summary>
    /// 서비스 이름
    /// </summary>
    public string ServiceName { get; set; } = "Demo.Web";

    /// <summary>
    /// 서비스 버전
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// 환경 정보 (Development, Staging, Production)
    /// </summary>
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// 서비스 인스턴스 ID
    /// </summary>
    public string? ServiceInstanceId { get; set; }

    /// <summary>
    /// 트레이싱 설정
    /// </summary>
    public TracingConfig Tracing { get; set; } = new();

    /// <summary>
    /// 메트릭 설정
    /// </summary>
    public MetricsConfig Metrics { get; set; } = new();

    /// <summary>
    /// 로깅 설정
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// 익스포터 설정
    /// </summary>
    public ExporterConfig Exporter { get; set; } = new();

    /// <summary>
    /// 리소스 제한 설정
    /// </summary>
    public ResourceLimitsConfig ResourceLimits { get; set; } = new();

    /// <summary>
    /// 성능 최적화 설정
    /// </summary>
    public PerformanceConfig Performance { get; set; } = new();
}

/// <summary>
/// 트레이싱 구성 설정
/// </summary>
public class TracingConfig
{
    /// <summary>
    /// 트레이싱 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 샘플링 비율 (0.0 ~ 1.0)
    /// </summary>
    public double SamplingRatio { get; set; } = 1.0;

    /// <summary>
    /// 최대 스팬 수
    /// </summary>
    public int MaxSpans { get; set; } = 2000;

    /// <summary>
    /// 최대 스팬 속성 수
    /// </summary>
    public int MaxAttributes { get; set; } = 128;

    /// <summary>
    /// 최대 스팬 이벤트 수
    /// </summary>
    public int MaxEvents { get; set; } = 128;

    /// <summary>
    /// 최대 스팬 링크 수
    /// </summary>
    public int MaxLinks { get; set; } = 128;

    /// <summary>
    /// 샘플링 전략 (TraceIdRatioBased, ParentBased, Adaptive)
    /// </summary>
    public string SamplingStrategy { get; set; } = "TraceIdRatioBased";

    /// <summary>
    /// 부모 기반 샘플링 활성화 여부
    /// </summary>
    public bool ParentBasedSampling { get; set; } = true;

    /// <summary>
    /// 헬스체크 엔드포인트 필터링 활성화 여부
    /// </summary>
    public bool FilterHealthChecks { get; set; } = true;

    /// <summary>
    /// 정적 파일 필터링 활성화 여부
    /// </summary>
    public bool FilterStaticFiles { get; set; } = true;

    /// <summary>
    /// 오류 기반 샘플링 활성화 여부 (오류가 발생한 트레이스는 항상 샘플링)
    /// </summary>
    public bool ErrorBasedSampling { get; set; } = true;

    /// <summary>
    /// 적응형 샘플링 활성화 여부
    /// </summary>
    public bool AdaptiveSampling { get; set; } = false;
}

/// <summary>
/// 메트릭 구성 설정
/// </summary>
public class MetricsConfig
{
    /// <summary>
    /// 메트릭 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 메트릭 수집 간격 (초)
    /// </summary>
    public int CollectionIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 배치 익스포트 타임아웃 (밀리초)
    /// </summary>
    public int ExportTimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// 최대 배치 크기
    /// </summary>
    public int MaxBatchSize { get; set; } = 512;

    /// <summary>
    /// 배치 익스포트 간격 (밀리초)
    /// </summary>
    public int BatchExportIntervalMilliseconds { get; set; } = 5000;

    /// <summary>
    /// 최대 메트릭 스트림 수
    /// </summary>
    public int MaxMetricStreams { get; set; } = 1000;

    /// <summary>
    /// 메트릭 스트림당 최대 메트릭 포인트 수
    /// </summary>
    public int MaxMetricPointsPerMetricStream { get; set; } = 2000;

    /// <summary>
    /// 메트릭 집계 임시성 (Cumulative, Delta)
    /// </summary>
    public string TemporalityPreference { get; set; } = "Delta";

    /// <summary>
    /// 메트릭 필터링 활성화 여부
    /// </summary>
    public bool EnableFiltering { get; set; } = true;
}

/// <summary>
/// 로깅 구성 설정
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// OpenTelemetry 로깅 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 트레이스 ID 포함 여부
    /// </summary>
    public bool IncludeTraceId { get; set; } = true;

    /// <summary>
    /// 스팬 ID 포함 여부
    /// </summary>
    public bool IncludeSpanId { get; set; } = true;

    /// <summary>
    /// 구조화된 로깅 활성화 여부
    /// </summary>
    public bool StructuredLogging { get; set; } = true;
}

/// <summary>
/// 익스포터 구성 설정
/// </summary>
public class ExporterConfig
{
    /// <summary>
    /// 익스포터 타입 (Console, OTLP, Jaeger)
    /// </summary>
    public string Type { get; set; } = "Console";

    /// <summary>
    /// 콘솔 익스포터 설정
    /// </summary>
    public ConsoleExporterConfig ConsoleExporter { get; set; } = new();

    /// <summary>
    /// OTLP 엔드포인트 URL
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// OTLP 헤더 설정
    /// </summary>
    public Dictionary<string, string> OtlpHeaders { get; set; } = new();

    /// <summary>
    /// OTLP 프로토콜 (grpc, http/protobuf)
    /// </summary>
    public string OtlpProtocol { get; set; } = "grpc";

    /// <summary>
    /// 익스포트 타임아웃 (밀리초)
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 10000;

    /// <summary>
    /// 재시도 정책 설정
    /// </summary>
    public RetryPolicyConfig RetryPolicy { get; set; } = new();

    /// <summary>
    /// 배치 익스포트 설정
    /// </summary>
    public BatchExportConfig BatchExport { get; set; } = new();
}

/// <summary>
/// 콘솔 익스포터 구성 설정
/// </summary>
public class ConsoleExporterConfig
{
    /// <summary>
    /// 콘솔 익스포터 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 포맷된 메시지 포함 여부
    /// </summary>
    public bool IncludeFormattedMessage { get; set; } = true;

    /// <summary>
    /// 스코프 포함 여부
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// 단일 라인 출력 여부
    /// </summary>
    public bool SingleLine { get; set; } = false;

    /// <summary>
    /// 타임스탬프 형식
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
}

/// <summary>
/// 재시도 정책 구성 설정
/// </summary>
public class RetryPolicyConfig
{
    /// <summary>
    /// 재시도 정책 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 최대 재시도 횟수
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// 초기 백오프 시간 (밀리초)
    /// </summary>
    public int InitialBackoffMs { get; set; } = 1000;

    /// <summary>
    /// 최대 백오프 시간 (밀리초)
    /// </summary>
    public int MaxBackoffMs { get; set; } = 30000;

    /// <summary>
    /// 백오프 배수
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
}

/// <summary>
/// 배치 익스포트 구성 설정
/// </summary>
public class BatchExportConfig
{
    /// <summary>
    /// 최대 큐 크기
    /// </summary>
    public int MaxQueueSize { get; set; } = 2048;

    /// <summary>
    /// 최대 익스포트 배치 크기
    /// </summary>
    public int MaxExportBatchSize { get; set; } = 512;

    /// <summary>
    /// 익스포트 타임아웃 (밀리초)
    /// </summary>
    public int ExportTimeoutMilliseconds { get; set; } = 30000;

    /// <summary>
    /// 스케줄 지연 시간 (밀리초)
    /// </summary>
    public int ScheduledDelayMilliseconds { get; set; } = 5000;
}

/// <summary>
/// 리소스 제한 구성 설정
/// </summary>
public class ResourceLimitsConfig
{
    /// <summary>
    /// 최대 메모리 사용량 (MB)
    /// </summary>
    public int MaxMemoryUsageMB { get; set; } = 512;

    /// <summary>
    /// 최대 CPU 사용률 (%)
    /// </summary>
    public int MaxCpuUsagePercent { get; set; } = 10;

    /// <summary>
    /// 최대 활성 스팬 수
    /// </summary>
    public int MaxActiveSpans { get; set; } = 10000;

    /// <summary>
    /// 최대 대기열 스팬 수
    /// </summary>
    public int MaxQueuedSpans { get; set; } = 50000;

    /// <summary>
    /// 스팬 프로세서 배치 크기
    /// </summary>
    public int SpanProcessorBatchSize { get; set; } = 2048;

    /// <summary>
    /// 스팬 프로세서 타임아웃 (밀리초)
    /// </summary>
    public int SpanProcessorTimeout { get; set; } = 30000;
}

/// <summary>
/// 성능 최적화 구성 설정
/// </summary>
public class PerformanceConfig
{
    /// <summary>
    /// GC 최적화 활성화 여부
    /// </summary>
    public bool EnableGCOptimization { get; set; } = true;

    /// <summary>
    /// 비동기 익스포트 사용 여부
    /// </summary>
    public bool UseAsyncExport { get; set; } = true;

    /// <summary>
    /// 압축 활성화 여부
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// 압축 타입 (gzip, deflate)
    /// </summary>
    public string CompressionType { get; set; } = "gzip";

    /// <summary>
    /// 헬스체크 필터링 활성화 여부
    /// </summary>
    public bool FilterHealthChecks { get; set; } = true;

    /// <summary>
    /// 정적 파일 필터링 활성화 여부
    /// </summary>
    public bool FilterStaticFiles { get; set; } = true;

    /// <summary>
    /// 최소 지속 시간 (밀리초) - 이보다 짧은 스팬은 필터링
    /// </summary>
    public int MinimumDurationMs { get; set; } = 10;
}