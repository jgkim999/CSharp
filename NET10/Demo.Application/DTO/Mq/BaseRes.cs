using MessagePack;

namespace Demo.Application.DTO.Mq;

[MessagePackObject]
public class BaseRes
{
    [Key(0)]
    public ResultCode ResultCode { get; set; }

    [Key(1)]
    public string Message { get; set; } = string.Empty;
}
