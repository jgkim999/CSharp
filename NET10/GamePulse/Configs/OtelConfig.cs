namespace GamePulse.Configs;

/// <summary>
/// 
/// </summary>
public class OtelConfig
{
    /// <summary>
    /// 
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    /// <summary>
    /// 
    /// </summary>
    public string TracesSamplerArg { get; set; } = string.Empty;
    /// <summary>
    /// 
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;
    /// <summary>
    /// 
    /// </summary>
    public string ServiceVersion { get; set; } = string.Empty;
}
