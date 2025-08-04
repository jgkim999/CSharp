using Mapster;

namespace Demo.Application.DTO.User;

public class UserDb
{
    public UserDb() { }

    // ReSharper disable once InconsistentNaming
    public long id { get; init; }
    // ReSharper disable once InconsistentNaming
    public string name { get; init; } = string.Empty;
    // ReSharper disable once InconsistentNaming
    public string email { get; init; } = string.Empty;
    // ReSharper disable once InconsistentNaming
    public string password { get; init; } = string.Empty;
    // ReSharper disable once InconsistentNaming
    public DateTime created_at { get; init; } = DateTime.Now;
}

public class UserDto
{
    public UserDto() { }

    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }  = DateTime.Now;
}

public class MapsterConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserDb, UserDto>()
            .Map(dest => dest.Id, src => src.id)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Email, src => src.email)
            .Map(dest => dest.CreatedAt, src => src.created_at)
            .TwoWays()
            .MapToConstructor(true);
    }
}
