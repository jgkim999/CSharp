using MessagePack;

namespace Demo.Application.DTO;

[MessagePackObject]
public class SocketMsgConnectionSuccessNfy
{
    [Key(0)]
    public string ConnectionId { get; set; } = string.Empty;
    
    [Key(1)]
    public DateTime ServerUtcTime { get; set; } = DateTime.UtcNow;
}
