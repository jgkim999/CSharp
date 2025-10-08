using System;
using System.Text;
using Demo.SimpleSocket.SuperSocket;
using MessagePack;

namespace Demo.SimpleSocketClient.Services;

/// <summary>
/// 메시지 수신 이벤트 인자
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public PacketFlags Flags { get; }
    public ushort MessageType { get; }
    public byte[] Body { get; }
    public string BodyText => Encoding.UTF8.GetString(Body);

    /// <summary>
    /// 압축 여부
    /// </summary>
    public bool IsCompressed => Flags.IsCompressed();

    /// <summary>
    /// 암호화 여부
    /// </summary>
    public bool IsEncrypted => Flags.IsEncrypted();

    public MessageReceivedEventArgs(PacketFlags flags, ushort messageType, byte[] body)
    {
        Flags = flags;
        MessageType = messageType;
        Body = body;
    }

    /// <summary>
    /// MessagePack 객체로 역직렬화
    /// </summary>
    public T? DeserializeMessagePack<T>()
    {
        if (Body.Length == 0)
            return default;

        return MessagePackSerializer.Deserialize<T>(Body);
    }
}
