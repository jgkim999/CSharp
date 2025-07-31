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
    /// <summary>
    /// Initializes a new instance of the SodBackgroundTaskQueue with an unbounded queue for SodCommand items.
    /// </summary>
    public SodBackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<SodCommand>();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <summary>
    /// Asynchronously adds a <see cref="SodCommand"/> to the background task queue.
    /// </summary>
    /// <param name="workItem">The command to enqueue for background processing.</param>
    public async Task EnqueueAsync(SodCommand workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <summary>
    /// Asynchronously retrieves the next <see cref="SodCommand"/> from the queue, supporting cancellation.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the dequeue operation.</param>
    /// <returns>The next <see cref="SodCommand"/> in the queue.</returns>
    public async Task<SodCommand> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
