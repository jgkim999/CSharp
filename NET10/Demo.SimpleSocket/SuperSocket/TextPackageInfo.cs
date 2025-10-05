namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// 텍스트 패킷 정보
/// 클라이언트로부터 받은 텍스트 메시지를 담는 클래스
/// </summary>
public class TextPackageInfo
{
    /// <summary>
    /// 수신된 텍스트 메시지
    /// </summary>
    public string Text { get; set; } = string.Empty;
}