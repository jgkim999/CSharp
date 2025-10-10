using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class VeryLongReq
{
    [Key(0)]
    public string Data { get; set; } = null!;
}
