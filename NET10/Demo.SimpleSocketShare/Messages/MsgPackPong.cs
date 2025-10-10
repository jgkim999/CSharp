using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class MsgPackPong
{
    [Key(0)]
    public DateTime ServerDt { get; set; }
}
