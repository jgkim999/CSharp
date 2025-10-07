using System.Collections.Concurrent;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

/// <summary>
/// MessageType별로 메시지 처리 핸들러를 등록하고 실행하는 클래스
/// </summary>
public class SocketMessageHandler
{
    private readonly ConcurrentDictionary<ushort, Func<BinaryPackageInfo, DemoSession, Task>> _handlers = new();
    private readonly ILogger<SocketMessageHandler> _logger;

    public SocketMessageHandler(ILogger<SocketMessageHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// MessageType에 대한 핸들러 등록
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
}