using System.Collections.Concurrent;
using Demo.Application.DTO.Socket;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using LiteBus.Commands.Abstractions;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

/// <summary>
/// MessageType별로 메시지 처리 핸들러를 등록하고 실행하는 클래스
/// </summary>
public class ClientSocketMessageHandler : IClientSocketMessageHandler
{
    private readonly ConcurrentDictionary<ushort, Func<BinaryPackageInfo, string, Task>> _handlers = new();
    private readonly ILogger<ClientSocketMessageHandler> _logger;
    private readonly ICommandMediator _mediator;
    private readonly ISessionManager _sessionManager;

    public ClientSocketMessageHandler(
        ILogger<ClientSocketMessageHandler> logger,
        ICommandMediator mediator,
        ISessionManager sessionManager)
    {
        _logger = logger;
        _mediator = mediator;
        _sessionManager = sessionManager;
        
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
        if (_handlers.TryGetValue(package.MessageType, out var handler))
        {
            try
            {
                await handler(package, sessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error handling message. SessionId: {SessionId}, MessageType: {MessageType}",
                    sessionId, package.MessageType);
                throw;
            }
        }

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
        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning("Session not found. SessionID: {SessionID}", sessionId);
            return null;
        }
        
        if (package.BodyLength == 0)
        {
            _logger.LogWarning("Received package with empty body. SessionID: {SessionID}", session.SessionID);
            return null;
        }

        try
        {
            ReadOnlyMemory<byte> bodyMemory = package.Body.AsMemory(0, package.BodyLength);
            byte[]? decryptedBuffer = null;
            byte[]? decompressedBuffer = null;
            int decryptedLength = 0;
            int decompressedLength = 0;

            _logger.LogInformation("[서버 수신] MessageType: {MessageType}, BodyLength: {BodyLength}, Flags: {Flags}, IsEncrypted: {IsEncrypted}, IsCompressed: {IsCompressed}",
                package.MessageType, package.BodyLength, package.Flags, package.Flags.IsEncrypted(), package.Flags.IsCompressed());

            try
            {
                // 1단계: 암호화된 데이터인 경우 먼저 복호화 (ArrayPool 사용)
                if (package.Flags.IsEncrypted())
                {
                    _logger.LogInformation("[서버 복호화 시작] BodyLength: {BodyLength}", bodyMemory.Length);
                    (decryptedBuffer, decryptedLength) = session.DecryptDataToPool(bodyMemory.Span);
                    bodyMemory = decryptedBuffer.AsMemory(0, decryptedLength);
                    _logger.LogInformation("[서버 복호화 완료] DecryptedLength: {DecryptedLength}", decryptedLength);
                }

                // 2단계: 압축된 데이터인 경우 압축 해제 (ArrayPool 사용)
                if (package.Flags.IsCompressed())
                {
                    _logger.LogInformation("[서버 압축 해제 시작] BodyLength: {BodyLength}", bodyMemory.Length);
                    (decompressedBuffer, decompressedLength) = session.DecompressDataToPool(bodyMemory.Span);
                    bodyMemory = decompressedBuffer.AsMemory(0, decompressedLength);
                    _logger.LogInformation("[서버 압축 해제 완료] DecompressedLength: {DecompressedLength}", decompressedLength);
                }

                // 3단계: MessagePack 역직렬화
                _logger.LogInformation("[서버 역직렬화 시작] FinalBodyLength: {FinalBodyLength}", bodyMemory.Length);
                var result = MessagePack.MessagePackSerializer.Deserialize<T>(bodyMemory);
                _logger.LogInformation("[서버 역직렬화 완료] Type: {TypeName}", typeof(T).Name);
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
