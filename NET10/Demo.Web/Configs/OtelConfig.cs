namespace Demo.Web.Configs;

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
}