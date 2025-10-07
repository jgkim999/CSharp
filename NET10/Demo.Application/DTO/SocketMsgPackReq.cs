using MessagePack;

namespace Demo.Application.DTO;

[MessagePackObject]
public class SocketMsgPackReq
{
    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public string Message { get; set; }
}
