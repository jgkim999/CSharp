using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class MsgPackPing
{
    [Key(0)]
    public DateTime ServerDt { get; set; } 
}
