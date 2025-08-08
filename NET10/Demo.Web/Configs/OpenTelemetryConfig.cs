namespace Demo.Web.Configs;

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
    /// 배치 익스포트 설정
    /// </summary>
    public BatchExportConfig BatchExport { get; set; } = new();
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