using System.Buffers.Binary;
using System.Threading.Channels;
using Demo.Application.DTO;
using MessagePack;
using SuperSocket.Connection;
using SuperSocket.Server;

namespace Demo.SimpleSocket.SuperSocket;

public class DemoSession : AppSession, IDisposable
{
    private readonly ILogger<DemoSession> _logger;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<BinaryPackageInfo> _receiveChannel = Channel.CreateUnbounded<BinaryPackageInfo>();
    private Task? _processTask;
    private bool _disposed;

    public DemoSession(ILogger<DemoSession> logger)
    {
        _logger = logger;
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

    /// <summary>Sends binary data to the client asynchronously.</summary>
    /// <param name="data">The binary data to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the async send operation.</returns>
    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        await Connection.SendAsync(data, cancellationToken);
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

                    // MessageType 1번: SocketMsgPackReq 처리
                    if (package.MessageType == 1 && package.BodyLength > 0)
                    {
                        try
                        {
                            // MessagePack 역직렬화
                            var request = MessagePackSerializer.Deserialize<SocketMsgPackReq>(package.Body.AsMemory(0, package.BodyLength));
                            _logger.LogInformation(
                                "SocketMsgPackReq 수신. Name: {Name}, Message: {Message}",
                                request.Name, request.Message);

                            // SocketMsgPackRes 생성
                            var response = new SocketMsgPackRes
                            {
                                Msg = $"서버에서 받은 메시지: {request.Message} (보낸이: {request.Name})",
                                ProcessDt = DateTime.Now
                            };

                            // MessagePack 직렬화
                            var responseBody = MessagePackSerializer.Serialize(response);
                            var responsePacket = new byte[4 + responseBody.Length];
                            var responseSpan = responsePacket.AsSpan();

                            // MessageType 2번으로 응답
                            BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(0, 2), 2);
                            BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(2, 2), (ushort)responseBody.Length);
                            responseBody.CopyTo(responseSpan.Slice(4));

                            await SendAsync(responsePacket, _cts.Token);

                            _logger.LogInformation("SocketMsgPackRes 전송 완료. Msg: {Msg}", response.Msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "MessagePack 처리 중 오류 발생");
                        }
                    }
                    else
                    {
                        // 기본 ECHO: 받은 패킷을 그대로 돌려보냄
                        byte[] response = new byte[4 + package.BodyLength];
                        var responseSpan = response.AsSpan();

                        // MessageType을 BigEndian으로 쓰기
                        BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(0, 2), package.MessageType);

                        // BodyLength를 BigEndian으로 쓰기
                        BinaryPrimitives.WriteUInt16BigEndian(responseSpan.Slice(2, 2), package.BodyLength);

                        // Body 복사 - BodySpan을 사용하여 실제 유효한 부분만 복사
                        if (package.BodyLength > 0)
                        {
                            package.BodySpan.CopyTo(responseSpan.Slice(4));
                        }

                        await SendAsync(response, _cts.Token);
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
