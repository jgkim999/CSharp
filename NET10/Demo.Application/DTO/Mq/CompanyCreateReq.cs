using MessagePack;

namespace Demo.Application.DTO.Mq;

[MessagePackObject]
public class CompanyCreateReq
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;
}

