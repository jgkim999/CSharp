using System.Diagnostics;

using GamePulse.Services;

namespace GamePulse.Sod.Services;

/// <summary>
/// Background worker that processes tasks from queue with multiple concurrent workers
/// </summary>
public class SodBackgroundWorker : BackgroundService
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SodBackgroundWorker> _logger;
    private readonly int _workerCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="SodBackgroundWorker"/> class with the specified service provider, background task queue, and logger.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve scoped services for background tasks.</param>
    /// <param name="taskQueue">The queue from which background work items are dequeued and executed.</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="workerCount">Number of concurrent workers (default: 3)</param>
    public SodBackgroundWorker(
        IServiceProvider serviceProvider,
        ISodBackgroundTaskQueue taskQueue,
        ILogger<SodBackgroundWorker> logger,
        int workerCount = 3)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _workerCount = workerCount;
    }
    
    /// <summary>
    /// Starts multiple concurrent workers to process background tasks from the queue.
    /// </summary>
    /// <param name="stoppingToken">Token used to signal cancellation of background processing.</param>
    /// <returns>A task representing the asynchronous execution of background work.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = new Task[_workerCount];
        
        for (int i = 0; i < _workerCount; i++)
        {
            int workerId = i;
            workers[i] = ProcessTasksAsync(workerId, stoppingToken);
        }
        
        await Task.WhenAll(workers);
    }
    
    /// <summary>
    /// Processes tasks for a single worker
    /// </summary>
    /// <param name="workerId">Worker identifier</param>
    /// <param name="stoppingToken">Cancellation token</param>
    private async Task ProcessTasksAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker {WorkerId} started", workerId);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);
                using var span = GamePulseActivitySource.StartActivity($"{nameof(SodBackgroundWorker)}-{workerId}", ActivityKind.Consumer, workItem.ParentActivity);
                await workItem.ExecuteAsync(_serviceProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} encountered an error", workerId);
            }
        }
        
        _logger.LogInformation("Worker {WorkerId} stopped", workerId);
    }
    
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
