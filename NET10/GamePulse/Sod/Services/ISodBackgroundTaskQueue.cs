using GamePulse.Sod.Commands;

namespace GamePulse.Sod.Services;

public interface ISodBackgroundTaskQueue
{
    /// <summary>
    /// Asynchronously adds a SodCommand work item to the background task queue.
    /// </summary>
    /// <param name="workItem"></param>
    Task EnqueueAsync(SodCommand workItem);

    /// <summary>
    /// Asynchronously retrieves and removes the next <see cref="SodCommand"/> from the background task queue.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the next <see cref="SodCommand"/> in the queue.</returns>
    Task<SodCommand> DequeueAsync(CancellationToken cancellationToken);
}
