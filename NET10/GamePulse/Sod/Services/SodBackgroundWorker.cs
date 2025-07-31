using System.Diagnostics;

using GamePulse.Services;

namespace GamePulse.Sod.Services;

/// <summary>
/// 
/// </summary>
public class SodBackgroundWorker : BackgroundService
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SodBackgroundWorker> _logger;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="taskQueue"></param>
    /// <summary>
    /// Initializes a new instance of the <see cref="SodBackgroundWorker"/> class with the specified service provider, background task queue, and logger.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve scoped services for background tasks.</param>
    /// <param name="taskQueue">The queue from which background work items are dequeued and executed.</param>
    public SodBackgroundWorker(IServiceProvider serviceProvider, ISodBackgroundTaskQueue taskQueue, ILogger<SodBackgroundWorker> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <summary>
    /// Continuously processes background tasks from the queue until cancellation is requested.
    /// </summary>
    /// <param name="stoppingToken">Token used to signal cancellation of background processing.</param>
    /// <returns>A task representing the asynchronous execution of background work.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);
            using var span = GamePulseActivitySource.StartActivity(nameof(SodBackgroundWorker), ActivityKind.Consumer, workItem.ParentActivity);
            await workItem.ExecuteAsync(_serviceProvider, stoppingToken);
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <summary>
    /// Initiates a graceful shutdown of the background worker, logging the shutdown event before completing the stop process.
    /// </summary>
    /// <param name="cancellationToken">Token to signal the shutdown request.</param>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SodBackgroundWorker is performing graceful shutdown.");
        await base.StopAsync(cancellationToken);
    }
}
