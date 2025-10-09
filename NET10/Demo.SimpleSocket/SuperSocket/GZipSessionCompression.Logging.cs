namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// GZipSessionCompression의 로깅 관련 partial 클래스
/// LoggerMessage 소스 생성기를 사용한 고성능 로깅
/// </summary>
public partial class GZipSessionCompression
{
    // LoggerMessage 소스 생성기 (고성능 로깅)
    [LoggerMessage(Level = LogLevel.Debug, Message = "[GZip 압축] 원본: {OriginalSize} 바이트 → 압축: {CompressedSize} 바이트 ({Ratio:F1}%)")]
    private partial void LogCompression(int originalSize, int compressedSize, double ratio);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[GZip 압축 해제] 압축: {CompressedSize} 바이트 → 원본: {OriginalSize} 바이트 ({Ratio:F1}%)")]
    private partial void LogDecompression(int compressedSize, int originalSize, double ratio);
}