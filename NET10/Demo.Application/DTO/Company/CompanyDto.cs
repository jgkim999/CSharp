using Demo.Domain.Entities;
using Mapster;

namespace Demo.Application.DTO.Company;

public class CompanyDto
{
    public CompanyDto() { }

    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CompanyMapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CompanyEntity, CompanyDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.CreatedAt, src => src.created_at)
            .TwoWays()
            .MapToConstructor(true);
    }
}