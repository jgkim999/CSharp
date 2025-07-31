using GamePulse.Sod.Commands;

namespace GamePulse.Sod.Services;

/// <summary>
/// 
/// </summary>
public interface ISodBackgroundTaskQueue
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="workItem"></param>
    /// <returns></returns>
    Task EnqueueAsync(SodCommand workItem);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SodCommand> DequeueAsync(CancellationToken cancellationToken);
}
