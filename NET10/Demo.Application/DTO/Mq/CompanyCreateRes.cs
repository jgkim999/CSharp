using MessagePack;

namespace Demo.Application.DTO.Mq;

[MessagePackObject]
public class CompanyCreateRes
{
    [Key(0)]
    public BaseRes Res { get; set; } = new();
    
    [Key(1)]
    public string CompanyId { get; set; } = string.Empty;
}
