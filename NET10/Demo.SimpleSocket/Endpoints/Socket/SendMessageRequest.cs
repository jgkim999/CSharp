namespace Demo.SimpleSocket.Endpoints.Socket;

/// <summary>
/// 소켓 메시지 전송 요청
/// </summary>
public class SendMessageRequest
{
    /// <summary>
    /// 메시지 타입 (0-65535)
    /// </summary>
    public ushort MessageType { get; set; }

    /// <summary>
    /// 전송할 메시지 (Base64 인코딩된 바이너리 데이터)
    /// </summary>
    public string Message { get; set; } = string.Empty;
}