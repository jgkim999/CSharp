using System.Collections.Concurrent;
using System.Diagnostics;
using Demo.Application.Services;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using Demo.SimpleSocketShare;
using Demo.SimpleSocketShare.Messages;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

/// <summary>
/// MessageType별로 메시지 처리 핸들러를 등록하고 실행하는 클래스
/// </summary>
public partial class ClientSocketMessageHandler : IClientSocketMessageHandler
{
    private readonly ConcurrentDictionary<ushort, Func<BinaryPackageInfo, string, Task>> _handlers = new();
    private readonly ILogger<ClientSocketMessageHandler> _logger;
    private readonly ICommandMediator _mediator;
    private readonly ISessionManager _sessionManager;
    private readonly ITelemetryService _telemetryService;

    public ClientSocketMessageHandler(
        ILogger<ClientSocketMessageHandler> logger,
        ICommandMediator mediator,
        ISessionManager sessionManager,
        ITelemetryService telemetryService)
    {
        _logger = logger;
        _mediator = mediator;
        _sessionManager = sessionManager;
        _telemetryService = telemetryService;

        RegisterHandler(SocketMessageType.Pong, OnPongAsync);
        RegisterHandler(SocketMessageType.MsgPackRequest, OnSocketMsgPackReqAsync);
        RegisterHandler(SocketMessageType.VeryLongReq, OnVeryLongReqAsync);
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 (BinaryPackageInfo, DemoSession) => Task</param>
    private void RegisterHandler(SocketMessageType messageType, Func<BinaryPackageInfo, string, Task> handler)
    {
        RegisterHandler((ushort)messageType, handler);
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록 (ushort 오버로드)
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 (BinaryPackageInfo, DemoSession) => Task</param>
    private void RegisterHandler(ushort messageType, Func<BinaryPackageInfo, string, Task> handler)
    {
        if (_handlers.TryAdd(messageType, handler))
        {
            _logger.LogInformation("Handler registered for MessageType: {MessageType}", messageType);
        }
        else
        {
            _logger.LogWarning("Handler already exists for MessageType: {MessageType}. Overwriting.", messageType);
            _handlers[messageType] = handler;
        }
    }

    /// <summary>
    /// 여러 MessageType에 대한 핸들러를 한 번에 등록
    /// </summary>
    public void RegisterHandlers(Dictionary<ushort, Func<BinaryPackageInfo, string, Task>> handlers)
    {
        foreach (var kvp in handlers)
        {
            RegisterHandler(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// MessageType에 해당하는 핸들러 실행
    /// </summary>
    /// <param name="package">패킷 정보</param>
    /// <param name="sessionId">세션</param>
    /// <returns>핸들러를 찾아 실행했으면 true, 없으면 false</returns>
    public async Task<bool> HandleAsync(BinaryPackageInfo package, string sessionId)
    {
        // 각 패킷 처리를 독립적인 root trace로 시작 (기존 trace와 연결 끊기)
        using var activity = _telemetryService.StartRootActivity(
            "MessageHandler.HandleAsync",
            ActivityKind.Consumer,
            new Dictionary<string, object?>
            {
                ["session.id"] = sessionId,
                ["message.type"] = package.MessageType,
                ["message.body_length"] = package.BodyLength,
                ["message.flags"] = package.Flags.ToString()
            });

        if (_handlers.TryGetValue(package.MessageType, out var handler))
        {
            try
            {
                await handler(package, sessionId);

                // 패킷 유형별 처리량 기록
                _telemetryService.RecordBusinessMetric(
                    "socket.message.processed",
                    1,
                    new Dictionary<string, object?>
                    {
                        ["message.type"] = package.MessageType,
                        ["message.type_name"] = ((SocketMessageType)package.MessageType).ToString(),
                        ["message.compressed"] = package.Flags.IsCompressed(),
                        ["message.encrypted"] = package.Flags.IsEncrypted()
                    });

                _telemetryService.SetActivitySuccess(activity, "Message handled successfully");
                return true;
            }
            catch (Exception ex)
            {
                // 에러 메트릭 기록
                _telemetryService.RecordBusinessMetric(
                    "socket.message.error",
                    1,
                    new Dictionary<string, object?>
                    {
                        ["message.type"] = package.MessageType,
                        ["message.type_name"] = ((SocketMessageType)package.MessageType).ToString(),
                        ["error.type"] = ex.GetType().Name
                    });

                _telemetryService.SetActivityError(activity, ex);
                _logger.LogError(ex,
                    "Error handling message. SessionId: {SessionId}, MessageType: {MessageType}",
                    sessionId, package.MessageType);
                throw;
            }
        }

        activity?.SetTag("handler.found", false);

        // 핸들러를 찾지 못한 메시지 기록
        _telemetryService.RecordBusinessMetric(
            "socket.message.no_handler",
            1,
            new Dictionary<string, object?>
            {
                ["message.type"] = package.MessageType
            });

        return false;
    }

    /// <summary>
    /// MessageType에 대한 핸들러 존재 여부 확인
    /// </summary>
    public bool HasHandler(ushort messageType) => _handlers.ContainsKey(messageType);

    /// <summary>
    /// MessageType에 대한 핸들러 제거
    /// </summary>
    public bool UnregisterHandler(ushort messageType)
    {
        return _handlers.TryRemove(messageType, out _);
    }

    /// <summary>
    /// 등록된 모든 핸들러 제거
    /// </summary>
    public void ClearHandlers()
    {
        _handlers.Clear();
        _logger.LogInformation("All handlers cleared");
    }

    /// <summary>
    /// 등록된 핸들러 개수
    /// </summary>
    public int HandlerCount => _handlers.Count;
    
    private T? CheckPackage<T>(BinaryPackageInfo package, string sessionId) where T : class
    {
        // CheckPackage는 HandleAsync의 child activity로 유지 (parentContext 지정 안함)
        using var activity = _telemetryService.StartActivity("MessageHandler.CheckPackage", ActivityKind.Internal, new Dictionary<string, object?>
        {
            ["session.id"] = sessionId,
            ["message.type"] = package.MessageType,
            ["message.body_length"] = package.BodyLength,
            ["message.flags"] = package.Flags.ToString(),
            ["message.target_type"] = typeof(T).Name
        });

        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found. SessionID: {SessionID}", sessionId);
            activity?.SetTag("error", "Session not found");
            return null;
        }

        if (package.BodyLength == 0)
        {
            _logger.LogWarning("Received package with empty body. SessionID: {SessionID}", session.SessionID);
            activity?.SetTag("error", "Empty body");
            return null;
        }

        try
        {
            ReadOnlyMemory<byte> bodyMemory = package.Body.AsMemory(0, package.BodyLength);
            byte[]? decryptedBuffer = null;
            byte[]? decompressedBuffer = null;
            int decryptedLength = 0;
            int decompressedLength = 0;
            int originalBodyLength = bodyMemory.Length;

            LogPackageReceived(package.MessageType, package.BodyLength, package.Flags);

            try
            {
                // 1단계: 암호화된 데이터인 경우 먼저 복호화 (ArrayPool 사용)
                if (package.Flags.IsEncrypted())
                {
                    (decryptedBuffer, decryptedLength) = session.DecryptDataToPool(bodyMemory.Span);
                    LogDecryption(originalBodyLength, decryptedLength);
                    bodyMemory = decryptedBuffer.AsMemory(0, decryptedLength);
                    originalBodyLength = decryptedLength;

                    activity?.SetTag("message.decrypted", true);
                    activity?.SetTag("message.decrypted_size", decryptedLength);
                }

                // 2단계: 압축된 데이터인 경우 압축 해제 (ArrayPool 사용)
                if (package.Flags.IsCompressed())
                {
                    (decompressedBuffer, decompressedLength) = session.DecompressDataToPool(bodyMemory.Span);
                    LogDecompression(originalBodyLength, decompressedLength);
                    bodyMemory = decompressedBuffer.AsMemory(0, decompressedLength);

                    activity?.SetTag("message.decompressed", true);
                    activity?.SetTag("message.decompressed_size", decompressedLength);
                }

                // 3단계: MessagePack 역직렬화
                var result = MessagePack.MessagePackSerializer.Deserialize<T>(bodyMemory);
                LogDeserialization(typeof(T).Name, bodyMemory.Length);

                _telemetryService.SetActivitySuccess(activity, "Package processed successfully");
                return result;
            }
            finally
            {
                // ArrayPool 버퍼 반환
                if (decryptedBuffer != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(decryptedBuffer);
                }
                if (decompressedBuffer != null)
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(decompressedBuffer);
                }
            }
        }
        catch (Exception ex)
        {
            _telemetryService.SetActivityError(activity, ex);
            _logger.LogError(
                ex,
                "Error deserializing package. SessionID: {SessionID}, MessageType: {MessageType}, BodyLength: {BodyLength}, Flags: {Flags}",
                session.SessionID, package.MessageType, package.BodyLength, package.Flags);
            return null;
        }
    }
    
    private async Task OnSocketMsgPackReqAsync(BinaryPackageInfo package, string sessionId)
    {
        var request = CheckPackage<MsgPackReq>(package, sessionId);
        if (request == null)
            return;
        await _mediator.SendAsync(new MsgPackReqCommand(request, sessionId));
    }
    
    private async Task OnPongAsync(BinaryPackageInfo package, string sessionId)
    {
        var request = CheckPackage<MsgPackPing>(package, sessionId);
        if (request == null)
            return;
        await _mediator.SendAsync(new PongCommand(request, sessionId));
    }

    /// <summary>
    /// VeryLongReq 메시지 처리 핸들러
    /// 압축 테스트를 위해 매우 긴 텍스트를 응답으로 전송
    /// </summary>
    private async Task OnVeryLongReqAsync(BinaryPackageInfo package, string sessionId)
    {
        var request = CheckPackage<VeryLongReq>(package, sessionId);
        if (request == null)
            return;
        await _mediator.SendAsync(new VeryLongReqCommand(request, sessionId));
    }
}
