using GamePulse.Sod.Commands;

using System.Threading.Channels;

namespace GamePulse.Sod.Services;

/// <summary>
/// Background task queue for Sod commands.
/// </summary>
public class SodBackgroundTaskQueue : ISodBackgroundTaskQueue
{
    private readonly Channel<SodCommand> _queue;
    /// <summary>
    /// 
    /// </summary>
    public SodBackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<SodCommand>();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="workItem"></param>
    public async Task EnqueueAsync(SodCommand workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SodCommand> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
