namespace Demo.SimpleSocket.SuperSocket.Interfaces;

/// <summary>
/// 클라이언트 소켓 메시지 핸들러 인터페이스
/// </summary>
public interface IClientSocketMessageHandler
{
    /// <summary>
    /// MessageType에 해당하는 핸들러 실행
    /// </summary>
    /// <param name="package">패킷 정보</param>
    /// <param name="sessionId">세션 ID</param>
    /// <returns>핸들러를 찾아 실행했으면 true, 없으면 false</returns>
    Task<bool> HandleAsync(BinaryPackageInfo package, string sessionId);

    /// <summary>
    /// 여러 MessageType에 대한 핸들러를 한 번에 등록
    /// </summary>
    void RegisterHandlers(Dictionary<ushort, Func<BinaryPackageInfo, string, Task>> handlers);

    /// <summary>
    /// MessageType에 대한 핸들러 존재 여부 확인
    /// </summary>
    bool HasHandler(ushort messageType);

    /// <summary>
    /// MessageType에 대한 핸들러 제거
    /// </summary>
    bool UnregisterHandler(ushort messageType);

    /// <summary>
    /// 등록된 모든 핸들러 제거
    /// </summary>
    void ClearHandlers();

    /// <summary>
    /// 등록된 핸들러 개수
    /// </summary>
    int HandlerCount { get; }
}