namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 소켓 메시지 타입 정의
/// </summary>
public enum SocketMessageType : ushort
{
    /// <summary>
    /// 연결 성공 메시지 (서버 -> 클라이언트)
    /// </summary>
    ConnectionSuccess = 0xFFFF,

    /// <summary>
    /// SocketMsgPackReq 메시지 (클라이언트 -> 서버)
    /// </summary>
    MsgPackRequest = 1,

    /// <summary>
    /// SocketMsgPackRes 메시지 (서버 -> 클라이언트)
    /// </summary>
    MsgPackResponse = 2,

    // 추가 메시지 타입을 여기에 정의
    // Example = 3,
}