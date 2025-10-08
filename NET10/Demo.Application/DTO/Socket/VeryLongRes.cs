using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class VeryLongRes
{
    [Key(0)]
    public string Data { get; set; } = string.Empty;
}
