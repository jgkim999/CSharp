using OpenTelemetry.Context.Propagation;

namespace JsonToLog.Features.LogSend;

public class LogSendTask
{
    public required Dictionary<string, object?> LogData { get; set; }
    public PropagationContext PropagationContext { get; set; }
}
