using System.Diagnostics;

using GamePulse.Services;
using GamePulse.Sod.Commands;

namespace GamePulse.Sod.Endpoints.Rtt;

/// <summary>
/// 
/// </summary>
public class RttCommand : SodCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientIp"></param>
    /// <param name="parentActivity"></param>
    public RttCommand(string clientIp, Activity? parentActivity) : base(clientIp, parentActivity)
    {
        ClientIp = clientIp;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="ct"></param>
    public override async Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var logger = serviceProvider.GetService<ILogger<RttCommand>>();
        using var span = GamePulseActivitySource.StartActivity(nameof(RttCommand), ActivityKind.Internal, parentActivity: ParentActivity);
        logger?.LogInformation("{ClientIp}", ClientIp);
        // RTT 처리
        await Task.CompletedTask;
    }
}
