using System.ComponentModel.DataAnnotations.Schema;

namespace Demo.Domain.Entities;

[Table("users")]
public class User
{
    public User() { }

    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

[Table("users")]
public class UserDb
{
    public UserDb() { }

    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
