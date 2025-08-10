namespace GamePulse.Configs;

public class OtelConfig
{
    public string Endpoint { get; set; } = string.Empty;

    public string TracesSamplerArg { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = string.Empty;
}
