using Demo.Domain.Entities;
using Mapster;

namespace Demo.Application.DTO.User;

public class UserDto
{
    public UserDto() { }

    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }  = DateTime.UtcNow;
}

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserEntity, UserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Email, src => src.email)
            .Map(dest => dest.CreatedAt, src => src.created_at)
            .TwoWays()
            .MapToConstructor(true);

        config.NewConfig<Demo.Domain.Entities.UserEntity, UserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Email, src => src.email)
            .Map(dest => dest.CreatedAt, src => src.created_at)
            .TwoWays()
            .MapToConstructor(true);
    }
}
