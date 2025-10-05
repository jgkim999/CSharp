using System.Collections.Concurrent;
using System.Text;
using SuperSocket.Connection;
using SuperSocket.Server.Abstractions.Session;

namespace Demo.SimpleSocket.SuperSocket;

public class SessionManager
{
    private readonly CancellationToken _cancellationToken;
    private readonly ILogger<SessionManager> _logger;
    private ConcurrentDictionary<string, DemoSession> _sessions = new();

    public SessionManager(IHostApplicationLifetime lifetime, ILogger<SessionManager> logger)
    {
        _cancellationToken = lifetime.ApplicationStopping;
        _logger = logger;
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

        // ECHO: 연결 성공 메시지를 클라이언트에게 전송
        var response = "ECHO: 연결 성공\r\n";
        await session.SendAsync(Encoding.UTF8.GetBytes(response), _cancellationToken);
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

        _sessions.TryRemove(session.SessionID, out _);

        await ValueTask.CompletedTask;
    }

    public async Task OnMessageAsync(IAppSession session, global::SuperSocket.ProtoBase.TextPackageInfo package)
    {
        _logger.LogInformation("SessionManager OnMessageAsync. SessionId: {SessionId}, Text: {Text}", session.SessionID, package.Text);

        // ECHO: 받은 메시지를 그대로 돌려보냄
        var response = "ECHO: " + package.Text + "\r\n";
        await session.SendAsync(Encoding.UTF8.GetBytes(response), _cancellationToken);
    }
}
