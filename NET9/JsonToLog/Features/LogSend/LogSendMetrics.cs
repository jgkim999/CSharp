using System.Diagnostics.Metrics;

namespace JsonToLog.Features.LogSend;

public class LogSendMetrics
{
    private readonly Histogram<double> _logSendDuration;
    private readonly Counter<long> _logSendCount;
    private readonly Counter<long> _logSendFailedCount;
        
    internal const string METER_NAME = "JsonToLog.LogSend";

    public LogSendMetrics(IMeterFactory factory)
    {
        var meter = factory.Create(METER_NAME);
        
        _logSendDuration = meter.CreateHistogram<double>(
            "log_send_duration_seconds",
            unit: "ms",
            description: "Duration of log send operations (ms)");
        
        _logSendCount = meter.CreateCounter<long>(
            "log_send_count",
            unit: "count",
            description: "Total number of log send operations");
        
        _logSendFailedCount = meter.CreateCounter<long>(
            "log_send_failed_count",
            unit: "count",
            description: "Total number of failed log send operations");
    }
    
    public void RecordLogSendDuration(TimeSpan elapsed)
    {
        _logSendCount.Add(1);
        _logSendDuration.Record(elapsed.TotalMilliseconds);
    }
    
    public void RecordLogSendFailed()
    {
        _logSendFailedCount.Add(1);
    }
}
