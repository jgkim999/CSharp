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
    /// <param name="logger"></param>
    public SodBackgroundWorker(IServiceProvider serviceProvider, ISodBackgroundTaskQueue taskQueue, ILogger<SodBackgroundWorker> logger)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="stoppingToken"></param>
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
    /// <param name="cancellationToken"></param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SodBackgroundWorker is performing graceful shutdown.");
        await base.StopAsync(cancellationToken);
    }
}
