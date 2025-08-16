using Demo.Application.Commands.Sod;

namespace Demo.Application.Services.Sod;

/// <summary>
/// SOD 백그라운드 작업 큐 인터페이스
/// </summary>
public interface ISodBackgroundTaskQueue
{
    /// <summary>
    /// SodCommand 작업 항목을 백그라운드 작업 큐에 비동기적으로 추가합니다
    /// </summary>
    /// <param name="workItem">큐에 추가할 작업 항목</param>
    /// <summary>
/// Asynchronously enqueues the specified SodCommand into the background task queue.
/// </summary>
/// <param name="workItem">The SodCommand to add to the queue.</param>
/// <returns>A Task that represents the asynchronous enqueue operation.</returns>
    Task EnqueueAsync(SodCommand workItem);

    /// <summary>
    /// 백그라운드 작업 큐에서 다음 SodCommand를 비동기적으로 검색하고 제거합니다
    /// </summary>
    /// <param name="cancellationToken">취소 요청을 모니터링하는 토큰</param>
    /// <summary>
/// Asynchronously dequeues the next <see cref="SodCommand"/> from the background queue.
/// </summary>
/// <param name="cancellationToken">Token to cancel waiting for an item; if canceled the returned task will observe cancellation.</param>
/// <returns>A task whose result is the next <see cref="SodCommand"/> from the queue.</returns>
    Task<SodCommand> DequeueAsync(CancellationToken cancellationToken);
}