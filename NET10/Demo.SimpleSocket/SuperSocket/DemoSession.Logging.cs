namespace Demo.SimpleSocket.SuperSocket;

/// <summary>
/// DemoSession의 로깅 관련 partial 클래스
/// LoggerMessage 소스 생성기를 사용한 고성능 로깅
/// </summary>
public partial class DemoSession
{
    // LoggerMessage 소스 생성기 (고성능 로깅)
    [LoggerMessage(Level = LogLevel.Debug, Message = "#1 DemoSession connected. {SessionID}")]
    private partial void LogSessionConnected(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "#3 DemoSession closed. {SessionID} {Reason}")]
    private partial void LogSessionClosed(string sessionId, string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[서버 압축] 원본: {OriginalSize} 바이트 → 압축: {CompressedSize} 바이트 ({Ratio:F1}%)")]
    private partial void LogCompression(int originalSize, int compressedSize, double ratio);

    [LoggerMessage(Level = LogLevel.Debug, Message = "DemoSession Reset. {SessionID}")]
    private partial void LogSessionReset(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "DemoSession. {SessionID}")]
    private partial void LogSessionClosing(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Package queued to channel. SessionID: {SessionID}, MessageType: {MessageType}, BodyLength: {BodyLength}, Sequence: {Sequence}, Reserved: {Reserved}")]
    private partial void LogPackageQueued(string sessionId, ushort messageType, ushort bodyLength, ushort sequence, byte reserved);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ping task started. SessionID: {SessionID}")]
    private partial void LogPingTaskStarted(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ping sent to client. SessionID: {SessionID}, ServerDt: {ServerDt}")]
    private partial void LogPingSent(string sessionId, DateTime serverDt);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Ping task cancelled. SessionID: {SessionID}")]
    private partial void LogPingTaskCancelled(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message processing task started. SessionID: {SessionID}")]
    private partial void LogProcessingTaskStarted(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SessionManager OnMessageAsync. SessionId: {SessionId}, MessageType: {MessageType}, BodyLength: {BodyLength}, Sequence: {Sequence}, Reserved: {Reserved}")]
    private partial void LogMessageReceived(string sessionId, ushort messageType, ushort bodyLength, ushort sequence, byte reserved);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message processing task cancelled. SessionID: {SessionID}")]
    private partial void LogProcessingTaskCancelled(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Message processing task stopped. SessionID: {SessionID}")]
    private partial void LogProcessingTaskStopped(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Cleaned up {Count} remaining packages in channel. SessionID: {SessionID}")]
    private partial void LogChannelCleanup(int count, string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Disposing DemoSession. SessionID: {SessionID}")]
    private partial void LogDisposingSession(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "DemoSession disposed successfully. SessionID: {SessionID}")]
    private partial void LogSessionDisposed(string sessionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[서버 암호화 초기화] KeySize: {KeySize}, IVSize: {IVSize}")]
    private partial void LogEncryptionInitialized(int keySize, int ivSize);
}