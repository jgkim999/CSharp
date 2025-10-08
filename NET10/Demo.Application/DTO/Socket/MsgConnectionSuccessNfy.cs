using MessagePack;

namespace Demo.Application.DTO.Socket;

[MessagePackObject]
public class MsgConnectionSuccessNfy
{
    [Key(0)]
    public string ConnectionId { get; set; } = string.Empty;
    
    [Key(1)]
    public DateTime ServerUtcTime { get; set; } = DateTime.UtcNow;
}
