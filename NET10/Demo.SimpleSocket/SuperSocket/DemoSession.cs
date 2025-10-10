using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Channels;
using Bogus;
using Demo.Application.Services;
using Demo.Application.Utils;
using Demo.SimpleSocket.Configs;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using Demo.SimpleSocketShare;
using Demo.SimpleSocketShare.Messages;
using MessagePack;
using Microsoft.Extensions.Options;
using SuperSocket.Connection;
using SuperSocket.ProtoBase;
using SuperSocket.Server;

namespace Demo.SimpleSocket.SuperSocket;

public partial class DemoSession : AppSession, IDisposable
{
    private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Shared;

    private readonly ILogger<DemoSession> _logger;
    private readonly IClientSocketMessageHandler _messageHandler;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISessionCompression _compression;
    private readonly ITelemetryService _telemetryService;
    private readonly CancellationTokenSource _cts = new();
    private readonly Channel<BinaryPackageInfo> _receiveChannel = Channel.CreateUnbounded<BinaryPackageInfo>();
    private readonly SequenceGenerator _seqGenerator = new();
    private readonly Faker _faker = new("ko");
    private readonly ArrayBufferWriter<byte> _bufferWriter = new(4096);
    private readonly SemaphoreSlim _bufferWriterLock = new(1, 1);  // ArrayBufferWriter 동기화용
    private readonly MsgPackPing _pingMsg = new();
    private Task? _processTask;
    private Task? _pingTask;
    private bool _disposed;
    private DateTime _lastPongUtc;
    private double _rttMs;
    private ISessionEncryption? _encryption;
    private readonly CustomServerOption _option;

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

    /// <summary>
    /// Dispose 여부
    /// </summary>
    public bool IsDisposed => _disposed;

    public DemoSession(
        ILogger<DemoSession> logger,
        IClientSocketMessageHandler messageHandler,
        ILoggerFactory loggerFactory,
        ISessionCompression compression,
        ITelemetryService telemetryService,
        IOptions<CustomServerOption> option)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _loggerFactory = loggerFactory;
        _compression = compression;
        _telemetryService = telemetryService;
        _option = option.Value;
    }

    /// <summary>
    /// Called when the session is connected.
    /// Session Call Sequence #1
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    protected override async ValueTask OnSessionConnectedAsync()
    {
        using var activity = _telemetryService.StartActivity("Session.Connected", ActivityKind.Server, new Dictionary<string, object?>
        {
            ["session.id"] = SessionID,
            ["remote.address"] = RemoteEndPoint?.ToString()
        });

        try
        {
            LogSessionConnected(SessionID);

            _lastPongUtc = DateTime.UtcNow;

            // 메시지 처리 백그라운드 태스크 시작
            _processTask = Task.Run(ProcessMessagesAsync);

            // Ping 전송 백그라운드 태스크 시작
            _pingTask = Task.Run(SendPingPeriodicallyAsync);

            _telemetryService.SetActivitySuccess(activity, "Session connected successfully");
            await ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            throw;
        }
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
        LogSessionClosed(SessionID, e.Reason.ToString());

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

    public async ValueTask SendMessagePackAsync<T>(SocketMessageType messageType, T msgPack, PacketFlags flags = PacketFlags.None, bool encrypt = false)
    {
        using var activity = _telemetryService.StartActivity("Session.SendMessagePack", ActivityKind.Producer, new Dictionary<string, object?>
        {
            ["session.id"] = SessionID,
            ["message.type"] = messageType.ToString(),
            ["message.encrypted"] = encrypt
        });

        // ArrayBufferWriter 동기화 (thread-safe)
        await _bufferWriterLock.WaitAsync(_cts.Token);

        try
        {
            _bufferWriter.Clear();  // 재사용을 위해 초기화
            MessagePackSerializer.Serialize(_bufferWriter, msgPack);
            ReadOnlyMemory<byte> bodyMemory = _bufferWriter.WrittenMemory;

            byte[]? compressedBuffer = null;
            byte[]? encryptedBuffer = null;
            int encryptedLength = 0;
            var originalSize = bodyMemory.Length;

            try
            {
                // 1단계: 압축 (512바이트 이상이면 자동 압축)
                if (bodyMemory.Length > SocketConst.AutoCompressThreshold)
                {
                    int compressedLength;
                    (compressedBuffer, compressedLength) = _compression.Compress(bodyMemory.Span, ArrayPool);
                    bodyMemory = compressedBuffer.AsMemory(0, compressedLength);
                    flags = flags.SetCompressed(true);

                    activity?.SetTag("message.compressed", true);
                    activity?.SetTag("message.original_size", originalSize);
                    activity?.SetTag("message.compressed_size", compressedLength);

                    LogCompression(originalSize, compressedLength, compressedLength * 100.0 / originalSize);
                }

                // 2단계: 암호화 (encrypt=true이면 암호화)
                if (encrypt)
                {
                    if (_encryption == null)
                    {
                        throw new InvalidOperationException("암호화가 초기화되지 않았습니다. GenerateAndSetEncryption()를 먼저 호출하세요.");
                    }

                    (encryptedBuffer, encryptedLength) = _encryption.Encrypt(bodyMemory.Span, ArrayPool);
                    bodyMemory = encryptedBuffer.AsMemory(0, encryptedLength);
                    flags = flags.SetEncrypted(true);

                    activity?.SetTag("message.encrypted_size", encryptedLength);
                }

                ushort bodyLength = (ushort)bodyMemory.Length;
                activity?.SetTag("message.final_size", bodyLength);

                await Connection.SendAsync(
                    (writer) =>
                    {
                        var headerSpan = writer.GetSpan(SocketConst.HeadSize);
                        headerSpan[SocketConst.FlagStart] = (byte)flags;
                        BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(SocketConst.SequenceStart, SocketConst.SequenceSize), _seqGenerator.GetNext());
                        headerSpan[SocketConst.ReservedStart] = _faker.Random.Byte();
                        BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(SocketConst.MessageTypeStart, SocketConst.MessageTypeSize), (ushort)messageType);
                        BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(SocketConst.BodySizeStart, SocketConst.BodySize), bodyLength);
                        writer.Advance(SocketConst.HeadSize);

                        var bodySpan = writer.GetSpan(bodyLength);
                        bodyMemory.Span.CopyTo(bodySpan);
                        writer.Advance(bodyMemory.Length);
                    }, _cts.Token);

                _telemetryService.SetActivitySuccess(activity, "Message sent successfully");
            }
            finally
            {
                if (compressedBuffer != null)
                {
                    ArrayPool.Return(compressedBuffer);
                }
                // 암호화 버퍼 ArrayPool 반환
                if (encryptedBuffer != null)
                {
                    ArrayPool.Return(encryptedBuffer);
                }
            }
        }
        catch (Exception ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            throw;
        }
        finally
        {
            // SemaphoreSlim 해제
            _bufferWriterLock.Release();
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
        LogSessionReset(SessionID);
    }

    /// <summary>Closes the session asynchronously.</summary>
    /// <returns>A task that represents the asynchronous close operation.</returns>
    public override async ValueTask CloseAsync()
    {
        LogSessionClosing(SessionID);
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

            LogPackageQueued(SessionID, package.MessageType, package.BodyLength, package.Sequence, package.Reserved);
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
        LogPingTaskStarted(SessionID);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_option.PingInterval), _cts.Token);

                // 재사용 가능한 객체 사용 (매번 생성하지 않음)
                _pingMsg.ServerDt = DateTime.UtcNow;

                await SendMessagePackAsync(SocketMessageType.Ping, _pingMsg);

                LogPingSent(SessionID, _pingMsg.ServerDt);
            }
        }
        catch (OperationCanceledException)
        {
            LogPingTaskCancelled(SessionID);
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
        LogProcessingTaskStarted(SessionID);

        try
        {
            await foreach (BinaryPackageInfo package in _receiveChannel.Reader.ReadAllAsync(_cts.Token))
            {
                // using을 사용하여 자동으로 ArrayPool 반환
                using (package)
                {
                    LogMessageReceived(SessionID, package.MessageType, package.BodyLength, package.Sequence, package.Reserved);

                    try
                    {
                        // 등록된 핸들러를 통해 메시지 처리
                        var handled = await _messageHandler.HandleAsync(package, SessionID);

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
            LogProcessingTaskCancelled(SessionID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in message processing task. SessionID: {SessionID}", SessionID);
        }
        finally
        {
            // Channel에 남아있는 모든 메시지를 Dispose
            await CleanupRemainingMessagesAsync();

            LogProcessingTaskStopped(SessionID);
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
            Span<byte> headerSpan = writer.GetSpan(SocketConst.HeadSize);
            headerSpan[SocketConst.FlagStart] = (byte)flags;  // 플래그 (1바이트)
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(SocketConst.SequenceStart, SocketConst.SequenceSize), sequence);  // 시퀀스 (2바이트)
            headerSpan[SocketConst.ReservedStart] = reserved;  // 예약 (1바이트)
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(SocketConst.MessageTypeStart, SocketConst.MessageTypeSize), messageType);  // 메시지 타입 (2바이트)
            BinaryPrimitives.WriteUInt16BigEndian(headerSpan.Slice(SocketConst.BodySizeStart, SocketConst.BodySize), bodyLength);   // 바디 길이 (2바이트)
            writer.Advance(SocketConst.HeadSize);

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
                LogChannelCleanup(cleanedCount, SessionID);
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
                LogDisposingSession(SessionID);

                // 1. CancellationTokenSource Dispose
                try
                {
                    _cts?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing CancellationTokenSource. SessionID: {SessionID}", SessionID);
                }

                // 2. Encryption Dispose
                try
                {
                    _encryption?.Dispose();
                    _encryption = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing Encryption. SessionID: {SessionID}", SessionID);
                }

                // 3. SemaphoreSlim Dispose
                try
                {
                    _bufferWriterLock?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing SemaphoreSlim. SessionID: {SessionID}", SessionID);
                }

                LogSessionDisposed(SessionID);
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
    /// 암호화 초기화 (AES 256비트)
    /// </summary>
    public (byte[] Key, byte[] IV) GenerateAndSetEncryption()
    {
        // 기존 암호화 인스턴스 해제
        _encryption?.Dispose();

        // 새로운 암호화 인스턴스 생성
        var encryptionLogger = _loggerFactory.CreateLogger<AesSessionEncryption>();
        _encryption = new AesSessionEncryption(encryptionLogger);

        LogEncryptionInitialized(_encryption.Key.Length, _encryption.IV.Length);

        return (_encryption.Key, _encryption.IV);
    }

    /// <summary>
    /// 데이터 복호화 (ArrayPool 사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <returns>(복호화된 버퍼, 실제 데이터 길이)</returns>
    public (byte[] Buffer, int Length) DecryptDataToPool(ReadOnlySpan<byte> encryptedData)
    {
        if (_encryption == null)
        {
            throw new InvalidOperationException("암호화가 초기화되지 않았습니다.");
        }

        return _encryption.Decrypt(encryptedData, ArrayPool);
    }

    /// <summary>
    /// 데이터 압축 해제 (ArrayPool 사용)
    /// 반환된 배열은 반드시 ArrayPool에 반환해야 함
    /// </summary>
    /// <returns>(압축 해제된 버퍼, 실제 데이터 길이)</returns>
    public (byte[] Buffer, int Length) DecompressDataToPool(ReadOnlySpan<byte> compressedData)
    {
        return _compression.Decompress(compressedData, ArrayPool);
    }
}
