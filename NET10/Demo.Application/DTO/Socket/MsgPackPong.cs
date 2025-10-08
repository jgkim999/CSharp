using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class MsgPackPong
{
    [Key(0)]
    public DateTime ServerDt { get; set; }
}
