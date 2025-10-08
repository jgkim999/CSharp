using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Threading.Channels;
using Bogus;
using Demo.Application.DTO.Socket;
using Demo.Application.Utils;
using Demo.SimpleSocket.SuperSocket.Handlers;
using MessagePack;
using Microsoft.IO;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using SuperSocket.Server;

namespace Demo.SimpleSocket.SuperSocket;

public class DemoSession : AppSession, IDisposable
{
    private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();
    private static readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;

    private readonly ILogger<DemoSession> _logger;
    private readonly ClientSocketMessageHandler _messageHandler;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<BinaryPackageInfo> _receiveChannel = Channel.CreateUnbounded<BinaryPackageInfo>();
    private readonly SequenceGenerator _seqGenerator = new();
    private readonly Faker _faker = new("ko");
    private Task? _processTask;
    private Task? _pingTask;
    private bool _disposed;
    private DateTime _lastPongUtc;
    private double _rttMs;

    /// <summary>
    /// 마지막 pong도달 시간
    /// </summary>
    public DateTime LastPongUtc => _lastPongUtc;
    
    /// <summary>
    /// 현재 시간에서 마지막 pong도달 후 지난시간
    /// </summary>
    /// <param name="utcNow"></param>
    /// <returns></returns>
    public double LastPongElapsedMs(DateTime utcNow) => (utcNow - _lastPongUtc).TotalMilliseconds;
    
    /// <summary>
    /// ping-pong에서 측정한 rtt
    /// </summary>
    public double RttMs => _rttMs;

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

        _lastPongUtc = DateTime.UtcNow;
        
        // 메시지 처리 백그라운드 태스크 시작
        _processTask = Task.Run(ProcessMessagesAsync);

        // Ping 전송 백그라운드 태스크 시작
        _pingTask = Task.Run(SendPingPeriodicallyAsync);

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

        // 메시지 처리 태스크 대기
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

        // Ping 태스크 대기
        if (_pingTask != null)
        {
            try
            {
                await _pingTask;
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while waiting for ping task completion");
            }
        }

        await ValueTask.CompletedTask;
    }

    public async ValueTask SendMessagePackAsync<T>(SocketMessageType messageType, T msgPack, PacketFlags flags = PacketFlags.None)
    {
        ArrayBufferWriter<byte> bufferWriter = new();
        MessagePackSerializer.Serialize(bufferWriter, msgPack);
        ReadOnlyMemory<byte> bodyMemory = bufferWriter.WrittenMemory;

        // 512바이트 이상이면 압축 수행
        byte[]? compressedBuffer = null;
        int compressedLength = 0;
        if (bodyMemory.Length > 512)
        {
            (compressedBuffer, compressedLength) = CompressData(bodyMemory.Span);
            bodyMemory = compressedBuffer.AsMemory(0, compressedLength);
            flags |= PacketFlags.Compressed;  // 압축 플래그 설정
        }

        try
        {
            ushort bodyLength = (ushort)bodyMemory.Length;
            await Connection.SendAsync(
                (writer) =>
                {
                    var headerSpan = writer.GetSpan(8);
                    headerSpan[0] = (byte)flags;  // 플래그 (1바이트)
                    BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(1, 2), _seqGenerator.GetNext());  // 시퀀스 (2바이트)
                    headerSpan[3] = _faker.Random.Byte();  // 예약 (1바이트)
                    BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(4, 2), (ushort)messageType);  // 메시지 타입 (2바이트)
                    BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(6, 2), bodyLength);  // 바디 길이 (2바이트)
                    writer.Advance(8);

                    // 바디 작성 - 직접 PipeWriter에 쓰기 (복사 1번만)
                    var bodySpan = writer.GetSpan(bodyLength);
                    bodyMemory.Span.CopyTo(bodySpan);
                    writer.Advance(bodyMemory.Length);
                }, _cts.Token);
        }
        finally
        {
            // 압축 버퍼 해제
            if (compressedBuffer != null)
            {
                _arrayPool.Return(compressedBuffer);
            }
        }
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
                "Package queued to channel. SessionID: {SessionID}, MessageType: {MessageType}, BodyLength: {BodyLength}, Sequence: {Sequence}, Reserved: {Reserved}",
                SessionID, package.MessageType, package.BodyLength, package.Sequence, package.Reserved);
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
    /// 5초마다 클라이언트에게 Ping 메시지를 전송하는 백그라운드 태스크
    /// </summary>
    private async Task SendPingPeriodicallyAsync()
    {
        _logger.LogInformation("Ping task started. SessionID: {SessionID}", SessionID);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);

                var pingMsg = new MsgPackPing
                {
                    ServerDt = DateTime.UtcNow
                };

                await SendMessagePackAsync(SocketMessageType.Ping, pingMsg);

                _logger.LogDebug("Ping sent to client. SessionID: {SessionID}, ServerDt: {ServerDt}",
                    SessionID, pingMsg.ServerDt);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Ping task cancelled. SessionID: {SessionID}", SessionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ping task. SessionID: {SessionID}", SessionID);
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
                        "SessionManager OnMessageAsync. SessionId: {SessionId}, MessageType: {MessageType}, BodyLength: {BodyLength}, Sequence: {Sequence}, Reserved: {Reserved}",
                        SessionID, package.MessageType, package.BodyLength, package.Sequence, package.Reserved);

                    try
                    {
                        // 등록된 핸들러를 통해 메시지 처리
                        var handled = await _messageHandler.HandleAsync(package, this);

                        // 핸들러가 없으면 기본 ECHO 처리
                        if (!handled)
                        {
                            await HandleEchoAsync(package);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Handle error. SessionID: {SessionID}", SessionID);
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
        var flags = package.Flags;
        var sequence = package.Sequence;
        var reserved = package.Reserved;
        var messageType = package.MessageType;
        var bodyLength = package.BodyLength;

        // PipeWriter를 사용하여 직접 전송 - 추가 메모리 할당 없음!
        await SendAsync(writer =>
        {
            // 헤더 작성 (8바이트)
            var headerSpan = writer.GetSpan(8);
            headerSpan[0] = (byte)flags;  // 플래그 (1바이트)
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(1, 2), sequence);  // 시퀀스 (2바이트)
            headerSpan[3] = reserved;  // 예약 (1바이트)
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(4, 2), messageType);  // 메시지 타입 (2바이트)
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(6, 2), bodyLength);   // 바디 길이 (2바이트)
            writer.Advance(8);

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

    public void SetLastPong(DateTime utcNow, double rttMs)
    {
        _lastPongUtc = utcNow;
        _rttMs = rttMs;
    }

    /// <summary>
    /// GZip을 사용하여 데이터 압축
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    /// <returns>(압축된 버퍼, 실제 압축 데이터 길이)</returns>
    private static (byte[] Buffer, int Length) CompressData(ReadOnlySpan<byte> data)
    {
        using var output = _memoryStreamManager.GetStream("DemoSession-Compress");
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(data);
        }

        // RecyclableMemoryStream에서 버퍼를 가져와 ArrayPool 버퍼로 복사
        var compressedLength = (int)output.Length;
        var result = _arrayPool.Rent(compressedLength);
        output.Position = 0;
        output.ReadExactly(result.AsSpan(0, compressedLength));
        return (result, compressedLength);
    }

    /// <summary>
    /// GZip으로 압축된 데이터 압축 해제
    /// RecyclableMemoryStream을 사용하여 메모리 할당 최소화
    /// </summary>
    private static byte[] DecompressData(ReadOnlySpan<byte> compressedData)
    {
        using var input = _memoryStreamManager.GetStream("DemoSession-Decompress-Input", compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = _memoryStreamManager.GetStream("DemoSession-Decompress-Output");

        gzip.CopyTo(output);
        return output.ToArray();
    }
}
