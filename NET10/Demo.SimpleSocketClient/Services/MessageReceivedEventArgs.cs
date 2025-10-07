using System;
using System.Text;
using MessagePack;

namespace Demo.SimpleSocketClient.Services;

/// <summary>
/// 메시지 수신 이벤트 인자
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    public ushort MessageType { get; }
    public byte[] Body { get; }
    public string BodyText => Encoding.UTF8.GetString(Body);

    public MessageReceivedEventArgs(ushort messageType, byte[] body)
    {
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
