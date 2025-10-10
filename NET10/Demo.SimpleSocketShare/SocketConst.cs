namespace Demo.SimpleSocketShare;

public static class SocketConst
{
    /// <summary>
    /// 고정 헤더 파이프라인 필터
    /// 패킷 구조:
    /// - 1바이트: 플래그 (압축, 암호화 등)
    /// - 2바이트: 시퀀스 번호 (ushort, BigEndian)
    /// - 1바이트: 예약됨 (미래 사용)
    /// - 2바이트: 메시지 타입 (ushort, BigEndian)
    /// - 2바이트: 바디 길이 (ushort, BigEndian)
    /// - N바이트: 바디 데이터
    /// </summary>
    public const int HeadSize = 8;
    
    /// <summary>
    /// 1바이트: 플래그 (압축, 암호화 등)
    /// </summary>
    public const int FlagSize = 1;
    public const int FlagStart = 0;
    
    /// <summary>
    /// 2바이트: 시퀀스 번호 (ushort, BigEndian)
    /// </summary>
    public const int SequenceSize = 2;
    public const int SequenceStart = 1;
    
    /// <summary>
    /// 1바이트: 예약됨 (미래 사용)
    /// </summary>
    public const int ReservedSize = 1;
    public const int ReservedStart = 3;
    
    /// <summary>
    /// 2바이트: 메시지 타입 (ushort, BigEndian)
    /// </summary>
    public const int MessageTypeSize = 2;
    public const int MessageTypeStart = 4;
    
    /// <summary>
    /// 2바이트: 바디 길이 (ushort, BigEndian)
    /// </summary>
    public const int BodySize = 2;
    public const int BodySizeStart = 6;
    
    /// <summary>
    /// 자동 압축 적용 크기 (byte)
    /// </summary>
    public const int AutoCompressThreshold = 512;
}
