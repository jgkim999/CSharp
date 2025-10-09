using Demo.Application.DTO.Socket;
using SuperSocket.Connection;
using SuperSocket.Server.Abstractions.Session;

namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 세션 관리 인터페이스
/// </summary>
public interface ISessionManager : IAsyncDisposable
{
    /// <summary>
    /// 세션 ID로 세션 가져오기
    /// </summary>
    DemoSession? GetSession(string sessionId);

    /// <summary>
    /// 세션 연결 시 호출
    /// </summary>
    Task OnConnectAsync(IAppSession session);

    /// <summary>
    /// 세션 연결 해제 시 호출
    /// </summary>
    Task OnDisconnectAsync(IAppSession session, CloseEventArgs closeReason);

    /// <summary>
    /// 메시지 수신 시 호출
    /// </summary>
    Task OnMessageAsync(IAppSession session, BinaryPackageInfo package);

    /// <summary>
    /// 여러 세션에게 메시지 전송
    /// </summary>
    ValueTask SendMessagePackAsync<T>(IEnumerable<string> sessionIds, SocketMessageType messageType, T msgPack);

    /// <summary>
    /// 특정 세션에게 메시지 전송
    /// </summary>
    ValueTask SendMessagePackAsync<T>(string sessionId, SocketMessageType messageType, T msgPack);

    /// <summary>
    /// 모든 세션에게 메시지 전송
    /// </summary>
    ValueTask SendMessagePackAllAsync<T>(SocketMessageType messageType, T msgPack);
}