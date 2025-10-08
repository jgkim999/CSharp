using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class MsgPackReq
{
    [Key(0)]
    public string Name { get; set; }

    [Key(1)]
    public string Message { get; set; }
}
