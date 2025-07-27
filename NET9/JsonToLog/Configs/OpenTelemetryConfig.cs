using Serilog.Sinks.OpenTelemetry;

namespace JsonToLog.Configs;

public class OpenTelemetryConfig
{
    public string Endpoint { get; set; } = "http://localhost:4317";
    public string Protocol { get; set; } = "OtlpProtocol.Grpc";
    public double TraceSampleRate { get; set; } = 0.1; // 10% trace rate

    public OtlpProtocol GetProtocol => Enum.TryParse<OtlpProtocol>(this.Protocol, true, out var protocol) ? protocol : OtlpProtocol.Grpc;
}
