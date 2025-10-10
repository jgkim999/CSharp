using System.Collections.Concurrent;
using Demo.SimpleSocket.SuperSocket.Interfaces;
using Demo.SimpleSocketShare;
using Demo.SimpleSocketShare.Messages;
using SuperSocket.Connection;
using SuperSocket.Server.Abstractions.Session;

namespace Demo.SimpleSocket.SuperSocket;

public class SessionManager : ISessionManager
{
    private readonly CancellationToken _cancellationToken;
    private readonly ILogger<SessionManager> _logger;
    private readonly ConcurrentDictionary<string, DemoSession> _sessions = new();
    
    public SessionManager(IHostApplicationLifetime lifetime, ILogger<SessionManager> logger)
    {
        _cancellationToken = lifetime.ApplicationStopping;
        _logger = logger;
    }
    
    public DemoSession? GetSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out DemoSession? session))
        {
            // Dispose된 세션은 null 반환
            if (session.IsDisposed)
            {
                _logger.LogWarning("Session is already disposed. SessionId: {SessionId}", sessionId);
                return null;
            }
            return session;
        }
        return null;
    }

    /// <summary>
    /// Session Call Sequence #2
    /// </summary>
    /// <param name="session"></param>
    public async Task OnConnectAsync(IAppSession session)
    {
        DemoSession? demoSession = session as DemoSession;
        if (demoSession is null)
        {
            _logger.LogError("Failed session casting. {SessionID}", session.SessionID);
            return;
        }
        _logger.LogInformation("#2 SessionManager OnConnectAsync. SessionId: {SessionId}", session.SessionID);

        _sessions.TryAdd(session.SessionID, demoSession);

        // 암호화 초기화
        var (aesKey, aesIv) = demoSession.GenerateAndSetEncryption();

        MsgConnectionSuccessNfy message = new()
        {
            ConnectionId = session.SessionID,
            ServerUtcTime = DateTime.UtcNow,
            AesKey = aesKey,
            AesIV = aesIv
        };

        _logger.LogInformation("AES Key/IV generated and sent. SessionId: {SessionId}, KeySize: {KeySize}, IVSize: {IVSize}",
            session.SessionID, aesKey.Length, aesIv.Length);

        await demoSession.SendMessagePackAsync(SocketMessageType.ConnectionSuccess, message);
    }

    public async ValueTask SendMessagePackAsync<T>(IEnumerable<string> sessionIds, SocketMessageType messageType, T msgPack)
    {
        foreach (var sessionId in sessionIds)
        {
            await SendMessagePackAsync(sessionId, messageType, msgPack);
        }
    }
    
    public async ValueTask SendMessagePackAsync<T>(string sessionId, SocketMessageType messageType, T msgPack)
    {
        if (_sessions.TryGetValue(sessionId, out DemoSession? demoSession))
        {
            await demoSession.SendMessagePackAsync(messageType, msgPack);
        }
    }
    
    public async ValueTask SendMessagePackAllAsync<T>(SocketMessageType messageType, T msgPack)
    {
        foreach (var session in _sessions)
        {
            await session.Value.SendMessagePackAsync(messageType, msgPack);
        }
    }

    /// <summary>
    /// Session Call Sequence #4
    /// </summary>
    /// <param name="session"></param>
    /// <param name="closeReason"></param>
    public async Task OnDisconnectAsync(IAppSession session, CloseEventArgs closeReason)
    {
        DemoSession? demoSession = session as DemoSession;
        if (demoSession is null)
        {
            _logger.LogError("Failed session casting. {SessionID}", session.SessionID);
            _sessions.TryRemove(session.SessionID, out _);
            return;
        }

        _logger.LogInformation("#4 SessionManager OnDisconnectAsync. SessionId: {SessionId}, CloseReason: {CloseReason}", session.SessionID, closeReason);

        // 세션 딕셔너리에서 제거
        _sessions.TryRemove(session.SessionID, out var removedSession);

        // 리소스 정리: IDisposable 패턴 호출
        if (removedSession != null)
        {
            try
            {
                removedSession.Dispose();
                _logger.LogInformation("DemoSession resources disposed. SessionId: {SessionId}", session.SessionID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing DemoSession. SessionId: {SessionId}", session.SessionID);
            }
        }

        await ValueTask.CompletedTask;
    }

    public async Task OnMessageAsync(IAppSession session, BinaryPackageInfo package)
    {
        if (_sessions.TryGetValue(session.SessionID, out DemoSession? demoSession))
        {
            await demoSession.OnReceiveAsync(package);
        }
        else
        {
            _logger.LogWarning("Received package but session not found. Disposing package to prevent memory leak. SessionId: {SessionId}", session.SessionID);
            // 세션을 찾지 못한 경우 package를 Dispose하여 ArrayPool 버퍼 반환 (메모리 누수 방지)
            package.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions.Values)
        {
            await session.CloseAsync();
            session.Dispose();
        }

        _sessions.Clear();
    }
}
