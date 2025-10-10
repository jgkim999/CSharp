using MessagePack;

namespace Demo.SimpleSocketShare.Messages;

[MessagePackObject]
public class MsgPackRes
{
    [Key(0)]
    public string Msg { get; set; }
    [Key(1)]
    public DateTime ProcessDt { get; set; }
}
