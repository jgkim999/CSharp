using System.Buffers.Binary;
using System.Collections.Concurrent;
using Demo.Application.DTO;
using MessagePack;
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

                    await session.SendAsync(responsePacket, _cancellationToken);

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

                await session.SendAsync(response, _cancellationToken);
            }
        }
    }
}
