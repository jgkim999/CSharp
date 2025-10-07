using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading.Channels;
using Demo.Application.DTO;
using Demo.SimpleSocket.SuperSocket.Handlers;
using MessagePack;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using SuperSocket.Server;

namespace Demo.SimpleSocket.SuperSocket;

public class DemoSession : AppSession, IDisposable
{
    private readonly ILogger<DemoSession> _logger;
    private readonly ClientSocketMessageHandler _messageHandler;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<BinaryPackageInfo> _receiveChannel = Channel.CreateUnbounded<BinaryPackageInfo>();
    private Task? _processTask;
    private bool _disposed;

    public DemoSession(ILogger<DemoSession> logger, ClientSocketMessageHandler messageHandler)
    {
        _logger = logger;
        _messageHandler = messageHandler;
    }

    /// <summary>
    /// Called when the session is connected.
    /// Session Call Sequence #1
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    protected override async ValueTask OnSessionConnectedAsync()
    {
        _logger.LogInformation("#1 DemoSession connected. {SessionID}", SessionID);

        // 메시지 처리 백그라운드 태스크 시작
        _processTask = Task.Run(ProcessMessagesAsync);

        await ValueTask.CompletedTask;
    }

    /// <summary>
    /// Called when the session is closed.
    /// Session Call Sequence #3
    /// </summary>
    /// <param name="e">The close event arguments containing the reason for closing.</param>
    /// <returns>
    /// A task representing the async operation.
    /// </returns>
    protected override async ValueTask OnSessionClosedAsync(CloseEventArgs e)
    {
        _logger.LogInformation("#3 DemoSession closed. {SessionID} {Reason}", SessionID, e.Reason.ToString());

        // Channel 정리 및 처리 태스크 종료
        await _cts.CancelAsync();
        _receiveChannel.Writer.Complete();

        if (_processTask != null)
        {
            try
            {
                await _processTask;
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while waiting for process task completion");
            }
        }

        await ValueTask.CompletedTask;
    }

    public async ValueTask SendMessagePackAsync<T>(SocketMessageType messageType, T msgPack)
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackSerializer.Serialize(bufferWriter, msgPack);
        ReadOnlyMemory<byte> bodyMemory = bufferWriter.WrittenMemory;
        ushort bodyLength = (ushort)bodyMemory.Length;
        await Connection.SendAsync(
            (writer) =>
            {
                var headerSpan = writer.GetSpan(4);
                BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(0, 2), (ushort)messageType);
                BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(2, 2), bodyLength);
                writer.Advance(4);
                
                // 바디 작성 - 직접 PipeWriter에 쓰기 (복사 1번만)
                var bodySpan = writer.GetSpan(bodyLength);
                bodyMemory.Span.CopyTo(bodySpan);
                writer.Advance(bodyMemory.Length);
            }, _cts.Token);
    }

    /// <summary>Sends binary data to the client asynchronously.</summary>
    /// <param name="data">The binary data to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async send operation.</returns>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await Connection.SendAsync(data, cancellationToken);
    }

    public async ValueTask SendAsync(ReadOnlySequence<byte> data, CancellationToken cancellationToken)
    {
        await Connection.SendAsync(data, cancellationToken);
    }

    public async ValueTask SendAsync<TPackage>(
        IPackageEncoder<TPackage> packageEncoder,
        TPackage package,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        await Connection.SendAsync(packageEncoder, package, cancellationToken);
    }

    public async ValueTask SendAsync(Action<PipeWriter> write, CancellationToken cancellationToken)
    {
        await Connection.SendAsync(write, cancellationToken);
    }

    /// <summary>
    /// Called when the session is reset. Derived classes can override this method to perform additional cleanup.
    /// </summary>
    protected override void Reset()
    {
        _logger.LogInformation("DemoSession Reset. {SessionID}", SessionID);
    }

    /// <summary>Closes the session asynchronously.</summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    public override async ValueTask CloseAsync()
    {
        _logger.LogInformation("DemoSession. {SessionID}", SessionID);
        await base.CloseAsync();
    }

    /// <summary>
    /// 패킷 수신 시 호출되며, Channel에 패킷을 추가합니다.
    /// </summary>
    public async ValueTask OnReceiveAsync(BinaryPackageInfo package)
    {
        try
        {
            // Channel에 패킷 추가 (비동기 대기 없이)
            await _receiveChannel.Writer.WriteAsync(package, _cts.Token);

            _logger.LogDebug(
                "Package queued to channel. SessionID: {SessionID}, MessageType: {MessageType}, BodyLength: {BodyLength}",
                SessionID, package.MessageType, package.BodyLength);
        }
        catch (OperationCanceledException)
        {
            // 세션이 취소되었을 때 패킷을 Dispose하여 ArrayPool 반환
            _logger.LogWarning("Session cancelled while queuing package. Disposing package. SessionID: {SessionID}", SessionID);
            package.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing package to channel. Disposing package. SessionID: {SessionID}", SessionID);
            package.Dispose();
        }
    }

    /// <summary>
    /// Channel에서 메시지를 하나씩 처리하는 백그라운드 태스크
    /// </summary>
    private async Task ProcessMessagesAsync()
    {
        _logger.LogInformation("Message processing task started. SessionID: {SessionID}", SessionID);

        try
        {
            await foreach (BinaryPackageInfo package in _receiveChannel.Reader.ReadAllAsync(_cts.Token))
            {
                // using을 사용하여 자동으로 ArrayPool 반환
                using (package)
                {
                    _logger.LogInformation(
                        "SessionManager OnMessageAsync. SessionId: {SessionId}, MessageType: {MessageType}, BodyLength: {BodyLength}",
                        SessionID, package.MessageType, package.BodyLength);

                    // 등록된 핸들러를 통해 메시지 처리
                    var handled = await _messageHandler.HandleAsync(package, this);

                    // 핸들러가 없으면 기본 ECHO 처리
                    if (!handled)
                    {
                        await HandleEchoAsync(package);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message processing task cancelled. SessionID: {SessionID}", SessionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in message processing task. SessionID: {SessionID}", SessionID);
        }
        finally
        {
            // Channel에 남아있는 모든 메시지를 Dispose
            await CleanupRemainingMessagesAsync();

            _logger.LogInformation("Message processing task stopped. SessionID: {SessionID}", SessionID);
        }
    }

    /// <summary>
    /// 기본 ECHO 핸들러: 받은 패킷을 그대로 돌려보냄
    /// PipeWriter를 사용하여 메모리 할당 최소화 (제로 카피)
    /// </summary>
    private async Task HandleEchoAsync(BinaryPackageInfo package)
    {
        // package.Body는 ArrayPool에서 관리되므로 Memory로 캡처
        var bodyMemory = package.Body.AsMemory(0, package.BodyLength);
        var messageType = package.MessageType;
        var bodyLength = package.BodyLength;

        // PipeWriter를 사용하여 직접 전송 - 추가 메모리 할당 없음!
        await SendAsync(writer =>
        {
            // 헤더 작성 (4바이트)
            var headerSpan = writer.GetSpan(4);
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(0, 2), messageType);
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(2, 2), bodyLength);
            writer.Advance(4);

            // Body 복사 - PipeWriter에 직접 쓰기 (복사 1번만)
            if (bodyLength > 0)
            {
                var bodySpan = writer.GetSpan(bodyLength);
                bodyMemory.Span.CopyTo(bodySpan);
                writer.Advance(bodyLength);
            }

        }, _cts.Token);
    }

    /// <summary>
    /// Channel에 남아있는 모든 BinaryPackageInfo를 Dispose하여 ArrayPool 누수 방지
    /// </summary>
    private async Task CleanupRemainingMessagesAsync()
    {
        var cleanedCount = 0;

        try
        {
            // Channel에서 남아있는 메시지를 모두 읽어서 Dispose
            while (_receiveChannel.Reader.TryRead(out var package))
            {
                try
                {
                    package.Dispose();
                    cleanedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing remaining package. SessionID: {SessionID}", SessionID);
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation(
                    "Cleaned up {Count} remaining packages in channel. SessionID: {SessionID}",
                    cleanedCount, SessionID);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during channel cleanup. SessionID: {SessionID}", SessionID);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// IDisposable 패턴 구현: 관리되지 않는 리소스 정리
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose 패턴의 핵심 메서드
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // 관리되는 리소스 정리
            try
            {
                _logger.LogInformation("Disposing DemoSession. SessionID: {SessionID}", SessionID);

                // 1. CancellationTokenSource Dispose
                try
                {
                    _cts?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing CancellationTokenSource. SessionID: {SessionID}", SessionID);
                }

                _logger.LogInformation("DemoSession disposed successfully. SessionID: {SessionID}", SessionID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DemoSession disposal. SessionID: {SessionID}", SessionID);
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// 소멸자 (Finalizer) - GC가 호출
    /// </summary>
    ~DemoSession()
    {
        Dispose(disposing: false);
    }
}
