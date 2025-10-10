namespace Demo.SimpleSocketShare;

/// <summary>
/// 패킷 플래그 (1바이트)
/// </summary>
[Flags]
public enum PacketFlags : byte
{
    /// <summary>
    /// 플래그 없음
    /// </summary>
    None = 0,

    /// <summary>
    /// 압축 여부 (1번째 비트)
    /// </summary>
    Compressed = 1 << 0, // 0x01

    /// <summary>
    /// 암호화 여부 (2번째 비트)
    /// </summary>
    Encrypted = 1 << 1, // 0x02

    // 나머지 비트는 미래를 위해 예약됨
    // Reserved3 = 1 << 2, // 0x04
    // Reserved4 = 1 << 3, // 0x08
    // Reserved5 = 1 << 4, // 0x10
    // Reserved6 = 1 << 5, // 0x20
    // Reserved7 = 1 << 6, // 0x40
    // Reserved8 = 1 << 7, // 0x80
}

/// <summary>
/// PacketFlags 확장 메서드
/// </summary>
public static class PacketFlagsExtensions
{
    /// <summary>
    /// 압축 여부 확인
    /// </summary>
    public static bool IsCompressed(this PacketFlags flags)
    {
        return (flags & PacketFlags.Compressed) == PacketFlags.Compressed;
    }

    /// <summary>
    /// 암호화 여부 확인
    /// </summary>
    public static bool IsEncrypted(this PacketFlags flags)
    {
        return (flags & PacketFlags.Encrypted) == PacketFlags.Encrypted;
    }

    /// <summary>
    /// 압축 플래그 설정
    /// </summary>
    public static PacketFlags SetCompressed(this PacketFlags flags, bool compressed)
    {
        return compressed
            ? flags | PacketFlags.Compressed
            : flags & ~PacketFlags.Compressed;
    }

    /// <summary>
    /// 암호화 플래그 설정
    /// </summary>
    public static PacketFlags SetEncrypted(this PacketFlags flags, bool encrypted)
    {
        return encrypted
            ? flags | PacketFlags.Encrypted
            : flags & ~PacketFlags.Encrypted;
    }
}