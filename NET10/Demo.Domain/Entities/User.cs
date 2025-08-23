namespace Demo.Domain.Entities;

public class User
{
    public User() { }

    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

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
    public DateTime created_at { get; init; } = DateTime.UtcNow;
}