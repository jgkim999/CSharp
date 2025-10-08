using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class VeryLongReq
{
    [Key(0)]
    public string Data { get; set; } = null!;
}
