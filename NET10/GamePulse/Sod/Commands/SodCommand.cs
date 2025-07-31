using System.Diagnostics;
using GamePulse.Commands;

namespace GamePulse.Sod.Commands;

/// <summary>
/// 
/// </summary>
public abstract class SodCommand : ICommandJob
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="clientIp"></param>
    /// <param name="activity"></param>
    protected SodCommand(string clientIp, Activity? parentActivity)
    {
        ClientIp = clientIp;
        ParentActivity = parentActivity;
    }

    /// <summary>
    /// 
    /// </summary>
    public string ClientIp { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    public Activity? ParentActivity { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken ct);
}
