using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class MsgPackRes
{
    [Key(0)]
    public string Msg { get; set; }
    [Key(1)]
    public DateTime ProcessDt { get; set; }
}
