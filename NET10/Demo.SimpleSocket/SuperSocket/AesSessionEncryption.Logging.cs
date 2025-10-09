namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// AesSessionEncryption의 로깅 관련 partial 클래스
/// LoggerMessage 소스 생성기를 사용한 고성능 로깅
/// </summary>
public partial class AesSessionEncryption
{
    // LoggerMessage 소스 생성기 (고성능 로깅)
    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 암호화 초기화] KeySize: {KeySize}, IVSize: {IVSize}")]
    private partial void LogEncryptionInitialized(int keySize, int ivSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 암호화] 원본: {OriginalSize} 바이트 → 암호화: {EncryptedSize} 바이트")]
    private partial void LogEncryption(int originalSize, int encryptedSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 복호화] 암호화: {EncryptedSize} 바이트 → 원본: {DecryptedSize} 바이트")]
    private partial void LogDecryption(int encryptedSize, int decryptedSize);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[AES 암호화 정리 완료]")]
    private partial void LogDisposed();
}