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

        // ECHO: 연결 성공 메시지를 클라이언트에게 바이너리 패킷 형식으로 전송
        // MessageType: 0xFFFF (연결 성공), BodyLength: 0
        byte[] response = new byte[4];
        response[0] = 0xFF; // MessageType 상위 바이트
        response[1] = 0xFF; // MessageType 하위 바이트
        response[2] = 0x00; // BodyLength 상위 바이트
        response[3] = 0x00; // BodyLength 하위 바이트

        await session.SendAsync(response, _cancellationToken);
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

    public async Task OnMessageAsync(IAppSession session, BinaryPackageInfo package)
    {
        // using을 사용하여 자동으로 ArrayPool 반환
        using (package)
        {
            _logger.LogInformation(
                "SessionManager OnMessageAsync. SessionId: {SessionId}, MessageType: {MessageType}, BodyLength: {BodyLength}",
                session.SessionID, package.MessageType, package.BodyLength);

            // ECHO: 받은 패킷을 그대로 돌려보냄
            // 응답 패킷 구성: MessageType(2) + BodyLength(2) + Body
            byte[] response = new byte[4 + package.BodyLength];

            // MessageType을 BigEndian으로 쓰기
            response[0] = (byte)(package.MessageType >> 8);
            response[1] = (byte)(package.MessageType & 0xFF);

            // BodyLength를 BigEndian으로 쓰기
            response[2] = (byte)(package.BodyLength >> 8);
            response[3] = (byte)(package.BodyLength & 0xFF);

            // Body 복사 - BodySpan을 사용하여 실제 유효한 부분만 복사
            if (package.BodyLength > 0)
            {
                package.BodySpan.CopyTo(response.AsSpan(4));
            }

            await session.SendAsync(response, _cancellationToken);
        }
    }
}
