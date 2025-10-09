using Demo.Application.DTO.Socket;

namespace Demo.SimpleSocket.SuperSocket.Handlers;

/// <summary>
/// ClientSocketMessageHandler의 로깅 관련 partial 클래스
/// LoggerMessage 소스 생성기를 사용한 고성능 로깅
/// </summary>
public partial class ClientSocketMessageHandler
{
    // LoggerMessage 소스 생성기 (고성능 로깅)
    [LoggerMessage(Level = LogLevel.Debug, Message = "[서버 수신] MessageType: {messageType}, BodyLength: {bodyLength}, Flags: {flags}")]
    private partial void LogPackageReceived(ushort messageType, ushort bodyLength, PacketFlags flags);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[서버 복호화] BodyLength: {bodyLength} → DecryptedLength: {decryptedLength}")]
    private partial void LogDecryption(int bodyLength, int decryptedLength);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[서버 압축 해제] BodyLength: {bodyLength} → DecompressedLength: {decompressedLength}")]
    private partial void LogDecompression(int bodyLength, int decompressedLength);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[서버 역직렬화] Type: {typeName}, BodyLength: {bodyLength}")]
    private partial void LogDeserialization(string typeName, int bodyLength);
}