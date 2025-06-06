using OpenTelemetry.Context.Propagation;

using System.Diagnostics;

namespace JsonToLog.Services;

public class LogSendTask
{
    public required Dictionary<string, object?> LogData { get; set; }
    public PropagationContext PropagationContext { get; set; }
}
