using GamePulse.Sod.Commands;
using System.Threading.Channels;

namespace GamePulse.Sod.Services;

/// <summary>
/// Thread-safe background task queue for Sod commands supporting multi-producer, multi-consumer pattern
/// </summary>
public class SodBackgroundTaskQueue : ISodBackgroundTaskQueue
{
    private readonly Channel<SodCommand> _queue;
    private readonly ChannelWriter<SodCommand> _writer;
    private readonly ChannelReader<SodCommand> _reader;
    
    /// <summary>
    /// Initializes a new instance of the SodBackgroundTaskQueue with an unbounded channel optimized for multi-producer, multi-consumer scenarios
    /// </summary>
    public SodBackgroundTaskQueue()
    {
        var options = new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };
        
        _queue = Channel.CreateUnbounded<SodCommand>(options);
        _writer = _queue.Writer;
        _reader = _queue.Reader;
    }
    
    /// <summary>
    /// Asynchronously adds a <see cref="SodCommand"/> to the background task queue in a thread-safe manner
    /// </summary>
    /// <param name="workItem">The command to enqueue for background processing</param>
    public async Task EnqueueAsync(SodCommand workItem)
    {
        await _writer.WriteAsync(workItem);
    }

    /// <summary>
    /// Asynchronously retrieves the next <see cref="SodCommand"/> from the queue in a thread-safe manner, supporting cancellation
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the dequeue operation</param>
    /// <returns>The next <see cref="SodCommand"/> in the queue</returns>
    public async Task<SodCommand> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _reader.ReadAsync(cancellationToken);
    }
    
    /// <summary>
    /// Attempts to enqueue a command synchronously without blocking
    /// </summary>
    /// <param name="workItem">The command to enqueue</param>
    /// <returns>True if the item was enqueued successfully, false otherwise</returns>
    public bool TryEnqueue(SodCommand workItem)
    {
        return _writer.TryWrite(workItem);
    }
    
    /// <summary>
    /// Attempts to dequeue a command synchronously without blocking
    /// </summary>
    /// <param name="workItem">The dequeued command if successful</param>
    /// <returns>True if an item was dequeued successfully, false otherwise</returns>
    public bool TryDequeue(out SodCommand? workItem)
    {
        return _reader.TryRead(out workItem);
    }
}
