using System.Diagnostics;
using Demo.Application.Services;
using GamePulse.Services;
using GamePulse.Sod.Commands;

namespace GamePulse.Sod.Endpoints.Rtt;

public class RttCommand : SodCommand
{
    private readonly int _rtt;
    private readonly int _quality;

    /// <summary>
    /// Initializes a new instance of the <see cref="RttCommand"/> class with the specified client IP address and optional parent activity.
    /// </summary>
    /// <param name="clientIp">The IP address of the client initiating the command.</param>
    /// <param name="quality"></param>
    /// <param name="parentActivity">An optional parent <see cref="Activity"/> for tracing context.</param>
    /// <param name="rtt"></param>
    public RttCommand(string clientIp, int rtt, int quality, Activity? parentActivity)
        : base(clientIp, parentActivity)
    {
        ClientIp = clientIp;
        _rtt = rtt;
        _quality = quality;
    }

    /// <summary>
    /// Executes the RTT command asynchronously, logging the client IP and initiating an activity span.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var logger = serviceProvider.GetService<ILogger<RttCommand>>();
        var ipToNationService = serviceProvider.GetService<IIpToNationService>();
        var telemetryService = serviceProvider.GetService<ITelemetryService>();
        using var span = GamePulseActivitySource.StartActivity(nameof(RttCommand), ActivityKind.Internal, parentActivity: ParentActivity);

        //span?.AddTag("ClientIp", ClientIp);
        // IP 주소를 국가 코드로 변환
        if (ipToNationService == null)
        {
            logger?.LogWarning("IpToNationService가 null입니다. 기본 국가 코드 'Unknown'을 사용합니다.");
            telemetryService?.RecordRttMetrics("Unknown", _rtt / (double)1000, _quality, "sod");
            logger?.LogInformation(
                "{Game} {ClientIp} {CountryCode} {Rtt} {Quality}",
                "sod", ClientIp, "Unknown", _rtt / (double)1000, _quality);
            return;
        }

        var countryCode = await ipToNationService.GetNationCodeAsync(ClientIp, ct);

        // RTT 처리 - 밀리초를 초 단위로 변환
        var rtt = _rtt / (double)1000;
        telemetryService?.RecordRttMetrics(countryCode, rtt, _quality, "sod");
        logger?.LogInformation(
            "{Game} {ClientIp} {CountryCode} {Rtt} {Quality}",
            "sod", ClientIp, countryCode, rtt, _quality);

        await Task.CompletedTask;
    }
}
