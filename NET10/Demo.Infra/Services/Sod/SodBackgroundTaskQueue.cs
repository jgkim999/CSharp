using Demo.Application.Services.Sod;
using System.Threading.Channels;
using Demo.Application.Handlers.Commands.Sod;

namespace Demo.Infra.Services.Sod;

/// <summary>
/// 다중 생산자, 다중 소비자 패턴을 지원하는 SOD 명령을 위한 스레드 안전 백그라운드 작업 큐
/// </summary>
public class SodBackgroundTaskQueue : ISodBackgroundTaskQueue
{
    private readonly Channel<SodCommand> _queue;
    private readonly ChannelWriter<SodCommand> _writer;
    private readonly ChannelReader<SodCommand> _reader;
    
    /// <summary>
    /// 다중 생산자, 다중 소비자 시나리오에 최적화된 무제한 채널로 SodBackgroundTaskQueue의 새 인스턴스를 초기화합니다
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
    /// 스레드 안전 방식으로 SodCommand를 백그라운드 작업 큐에 비동기적으로 추가합니다
    /// </summary>
    /// <param name="workItem">백그라운드 처리를 위해 큐에 추가할 명령</param>
    /// <returns>비동기 작업</returns>
    public async Task EnqueueAsync(SodCommand workItem)
    {
        await _writer.WriteAsync(workItem);
    }

    /// <summary>
    /// 취소를 지원하는 스레드 안전 방식으로 큐에서 다음 SodCommand를 비동기적으로 검색합니다
    /// </summary>
    /// <param name="cancellationToken">큐 해제 작업을 취소하는 토큰</param>
    /// <returns>큐의 다음 SodCommand</returns>
    public async Task<SodCommand> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _reader.ReadAsync(cancellationToken);
    }
    
    /// <summary>
    /// 차단하지 않고 동기적으로 명령을 큐에 추가하려고 시도합니다
    /// </summary>
    /// <param name="workItem">큐에 추가할 명령</param>
    /// <returns>항목이 성공적으로 큐에 추가되면 true, 그렇지 않으면 false</returns>
    public bool TryEnqueue(SodCommand workItem)
    {
        return _writer.TryWrite(workItem);
    }
    
    /// <summary>
    /// 차단하지 않고 동기적으로 명령을 큐에서 제거하려고 시도합니다
    /// </summary>
    /// <param name="workItem">성공하면 큐에서 제거된 명령</param>
    /// <returns>항목이 성공적으로 큐에서 제거되면 true, 그렇지 않으면 false</returns>
    public bool TryDequeue(out SodCommand? workItem)
    {
        return _reader.TryRead(out workItem);
    }
}