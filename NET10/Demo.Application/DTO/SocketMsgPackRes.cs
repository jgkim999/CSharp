using MessagePack;

namespace Demo.Application.DTO;

[MessagePackObject]
public class SocketMsgPackRes
{
    [Key(0)]
    public string Msg { get; set; }
    [Key(1)]
    public DateTime ProcessDt { get; set; }
}
