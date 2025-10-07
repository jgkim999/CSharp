using System.Buffers;
using System.Collections.Concurrent;
using Demo.Application.DTO;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

/// <summary>
/// MessageType별로 메시지 처리 핸들러를 등록하고 실행하는 클래스
/// </summary>
public class ClientSocketMessageHandler
{
    private readonly ConcurrentDictionary<ushort, Func<BinaryPackageInfo, DemoSession, Task>> _handlers = new();
    private readonly ILogger<ClientSocketMessageHandler> _logger;

    public ClientSocketMessageHandler(ILogger<ClientSocketMessageHandler> logger)
    {
        _logger = logger;
        RegisterHandler(SocketMessageType.MsgPackRequest, OnSocketMsgPackReqAsync);
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 (BinaryPackageInfo, DemoSession) => Task</param>
    public void RegisterHandler(SocketMessageType messageType, Func<BinaryPackageInfo, DemoSession, Task> handler)
    {
        RegisterHandler((ushort)messageType, handler);
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록 (ushort 오버로드)
    /// </summary>
    /// <param name="messageType">메시지 타입</param>
    /// <param name="handler">처리 함수 (BinaryPackageInfo, DemoSession) => Task</param>
    public void RegisterHandler(ushort messageType, Func<BinaryPackageInfo, DemoSession, Task> handler)
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
    public void RegisterHandlers(Dictionary<ushort, Func<BinaryPackageInfo, DemoSession, Task>> handlers)
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
    /// <param name="session">세션</param>
    /// <returns>핸들러를 찾아 실행했으면 true, 없으면 false</returns>
    public async Task<bool> HandleAsync(BinaryPackageInfo package, DemoSession session)
    {
        if (_handlers.TryGetValue(package.MessageType, out var handler))
        {
            try
            {
                await handler(package, session);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error handling message. SessionId: {SessionId}, MessageType: {MessageType}",
                    session.SessionID, package.MessageType);
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
    
    private async Task OnSocketMsgPackReqAsync(BinaryPackageInfo package, DemoSession session)
    {
        if (package.BodyLength == 0)
        {
            _logger.LogWarning("SocketMsgPackReq received but BodyLength is 0. SessionID: {SessionID}", session.SessionID);
            return;
        }

        try
        {
            // MessagePack 역직렬화 - package.Body는 이미 ArrayPool을 사용하므로 추가 할당 없음
            var request = MessagePack.MessagePackSerializer.Deserialize<Application.DTO.SocketMsgPackReq>(
                package.Body.AsMemory(0, package.BodyLength));

            _logger.LogInformation("SocketMsgPackReq 수신. Name: {Name}, Message: {Message}",
                request.Name, request.Message);

            // SocketMsgPackRes 생성
            var response = new SocketMsgPackRes
            {
                Msg = $"서버에서 받은 메시지: {request.Message} (보낸이: {request.Name})",
                ProcessDt = DateTime.Now
            };

            await session.SendMessagePackAsync(SocketMessageType.MsgPackResponse, response);
            _logger.LogInformation("SocketMsgPackRes 전송 완료. Msg: {Msg}", response.Msg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MessagePack 처리 중 오류 발생. SessionID: {SessionID}", session.SessionID);
        }
    }
}
