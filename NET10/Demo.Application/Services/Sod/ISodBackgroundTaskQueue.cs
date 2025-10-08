using Demo.Application.Handlers.Commands.Sod;

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
    /// <returns>비동기 작업</returns>
    Task EnqueueAsync(SodCommand workItem);

    /// <summary>
    /// 백그라운드 작업 큐에서 다음 SodCommand를 비동기적으로 검색하고 제거합니다
    /// </summary>
    /// <param name="cancellationToken">취소 요청을 모니터링하는 토큰</param>
    /// <returns>큐의 다음 SodCommand를 포함하는 비동기 작업</returns>
    Task<SodCommand> DequeueAsync(CancellationToken cancellationToken);
}