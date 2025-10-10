using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class MsgPackPing
{
    [Key(0)]
    public DateTime ServerDt { get; set; } 
}
