using System.Diagnostics.Metrics;
using GamePulse.Configs;
using Microsoft.Extensions.Options;

namespace GamePulse.Sod.Metrics;

public class SodMetrics : IDisposable
{
    public static string M1 = "metric1";

    private readonly Meter _meter1;
    private readonly Counter<long> _rttCount;
    private readonly Histogram<double> _rttHistogram;
    private readonly Histogram<double> _networkQuality;
    private readonly Gauge<double> _rttGauge;

    public SodMetrics(IOptions<OtelConfig> config)
    {
        _meter1 = new Meter(M1);

        //_meter = meterFactory.Create(config.Value.ServiceName);
        _rttCount = _meter1.CreateCounter<long>("rtt_count", description: "rtt call count");
        _networkQuality = _meter1.CreateHistogram<double>(
            name: "rtt_quality",
            unit: "number",
            description: "Quality of network");
        _rttHistogram = _meter1.CreateHistogram<double>(
            name: "rtt_duration",
            unit: "s",
            description: "Duration of rtt");
        _rttGauge = _meter1.CreateGauge<double>("rtt_ping");
    }

    public void AddRtt(string countryCode, double rtt, double quality)
    {
        KeyValuePair<string, object>[] tags =
        [
            new("country", countryCode),
            new("game", "sod")
        ];
        _rttCount.Add(1, tags!);
        _rttHistogram.Record(rtt, tags!);
        _networkQuality.Record(quality, tags!);
        _rttGauge.Record(rtt, tags!);
    }

    public void Dispose()
    {
        _meter1.Dispose();
    }
}
