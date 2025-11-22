using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class MsgPackReq
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;

    [Key(1)]
    public string Message { get; set; } = string.Empty;
}
