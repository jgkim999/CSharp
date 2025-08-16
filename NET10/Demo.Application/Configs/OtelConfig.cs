namespace Demo.Application.Configs;

/// <summary>
/// OpenTelemetry 설정
/// </summary>
public class OtelConfig
{
    /// <summary>
    /// OpenTelemetry 엔드포인트
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// 트레이스 샘플러 인수
    /// </summary>
    public string TracesSamplerArg { get; set; } = string.Empty;

    /// <summary>
    /// 서비스 이름
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// 서비스 버전
    /// </summary>
    public string ServiceVersion { get; set; } = string.Empty;

    /// <summary>
    /// 서비스 네임스페이스 (예: production, staging, development)
    /// </summary>
    public string ServiceNamespace { get; set; } = "production";

    /// <summary>
    /// 서비스 인스턴스 ID
    /// </summary>
    public string ServiceInstanceId { get; set; } = Environment.MachineName;

    /// <summary>
    /// 배포 환경 (예: aws, azure, on-premises)
    /// </summary>
    public string DeploymentEnvironment { get; set; } = "aws";

    /// <summary>
    /// Prometheus 메트릭 익스포트 활성화 여부
    /// </summary>
    public bool EnablePrometheusExporter { get; set; } = true;

    /// <summary>
    /// 콘솔 익스포터 활성화 여부 (개발 환경용)
    /// </summary>
    public bool EnableConsoleExporter { get; set; } = false;

    /// <summary>
    /// 메트릭 익스포트 간격 (밀리초)
    /// </summary>
    public int MetricExportIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 배치 익스포트 타임아웃 (밀리초)
    /// </summary>
    public int BatchExportTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// 최대 배치 크기
    /// </summary>
    public int MaxBatchSize { get; set; } = 512;
}