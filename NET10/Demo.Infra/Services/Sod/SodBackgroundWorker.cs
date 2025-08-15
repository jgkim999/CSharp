using System.Diagnostics;
using Demo.Application.Services.Sod;
using Demo.Infra.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Infra.Services.Sod;

/// <summary>
/// 여러 동시 작업자로 큐에서 작업을 처리하는 백그라운드 워커
/// </summary>
public class SodBackgroundWorker : BackgroundService
{
    private readonly ISodBackgroundTaskQueue _taskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SodBackgroundWorker> _logger;
    private readonly int _workerCount;

    /// <summary>
    /// 지정된 서비스 공급자, 백그라운드 작업 큐 및 로거로 SodBackgroundWorker 클래스의 새 인스턴스를 초기화합니다
    /// </summary>
    /// <param name="serviceProvider">백그라운드 작업에 대한 범위 지정 서비스를 해결하는 데 사용되는 서비스 공급자</param>
    /// <param name="taskQueue">백그라운드 작업 항목이 큐에서 제거되고 실행되는 큐</param>
    /// <param name="logger">로거 인스턴스</param>
    /// <param name="workerCount">동시 작업자 수 (기본값: 8)</param>
    public SodBackgroundWorker(
        IServiceProvider serviceProvider,
        ISodBackgroundTaskQueue taskQueue,
        ILogger<SodBackgroundWorker> logger,
        int workerCount = 8)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _workerCount = workerCount;
    }

    /// <summary>
    /// 큐에서 백그라운드 작업을 처리하기 위해 여러 동시 작업자를 시작합니다
    /// </summary>
    /// <param name="stoppingToken">백그라운드 처리 취소를 신호하는 데 사용되는 토큰</param>
    /// <returns>백그라운드 작업의 비동기 실행을 나타내는 작업</returns>
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
    /// 단일 작업자에 대한 작업을 처리합니다
    /// </summary>
    /// <param name="workerId">작업자 식별자</param>
    /// <param name="stoppingToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    private async Task ProcessTasksAsync(int workerId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("작업자 {WorkerId}가 시작되었습니다", workerId);

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
                _logger.LogError(ex, "작업자 {WorkerId}에서 오류가 발생했습니다", workerId);
            }
        }

        _logger.LogInformation("작업자 {WorkerId}가 중지되었습니다", workerId);
    }

    /// <summary>
    /// 중지 프로세스를 완료하기 전에 종료 이벤트를 로깅하여 백그라운드 워커의 정상적인 종료를 시작합니다
    /// </summary>
    /// <param name="cancellationToken">종료 요청을 신호하는 토큰</param>
    /// <returns>비동기 중지 작업을 나타내는 작업</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SodBackgroundWorker가 정상적인 종료를 수행하고 있습니다.");
        await base.StopAsync(cancellationToken);
    }
}