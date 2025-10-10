using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class MsgConnectionSuccessNfy
{
    [Key(0)]
    public string ConnectionId { get; set; } = string.Empty;

    [Key(1)]
    public DateTime ServerUtcTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// AES 암호화 키 (256비트, 32바이트)
    /// </summary>
    [Key(2)]
    public byte[] AesKey { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// AES 초기화 벡터 (128비트, 16바이트)
    /// </summary>
    [Key(3)]
    public byte[] AesIV { get; set; } = Array.Empty<byte>();
}
