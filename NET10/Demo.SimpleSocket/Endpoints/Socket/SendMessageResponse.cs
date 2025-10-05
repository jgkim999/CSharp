namespace Demo.SimpleSocket.Endpoints.Socket;

/// <summary>
/// 소켓 메시지 전송 응답
/// </summary>
public class SendMessageResponse
{
    /// <summary>
    /// 성공 여부
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 응답 메시지 타입
    /// </summary>
    public ushort MessageType { get; set; }

    /// <summary>
    /// 응답 바디 길이
    /// </summary>
    public ushort BodyLength { get; set; }

    /// <summary>
    /// 서버로부터 받은 응답 메시지 (Base64 인코딩된 바이너리 데이터)
    /// </summary>
    public string ResponseMessage { get; set; } = string.Empty;

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }
}