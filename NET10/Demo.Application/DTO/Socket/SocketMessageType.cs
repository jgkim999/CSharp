namespace Demo.Application.DTO.Socket;

/// <summary>
/// 소켓 메시지 타입 정의
/// </summary>
public enum SocketMessageType : ushort
{
    /// <summary>
    /// 연결 성공 메시지 (서버 -> 클라이언트)
    /// </summary>
    ConnectionSuccess = 1,
    
    Ping = 2,
    Pong = 3,

    /// <summary>
    /// MsgPackReq 메시지 (클라이언트 -> 서버)
    /// </summary>
    MsgPackRequest = 4,

    /// <summary>
    /// MsgPackRes 메시지 (서버 -> 클라이언트)
    /// </summary>
    MsgPackResponse = 5,
    
    VeryLongReq,
    VeryLongRes,
}
