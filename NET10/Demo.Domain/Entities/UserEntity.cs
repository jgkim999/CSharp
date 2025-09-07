using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable InconsistentNaming

namespace Demo.Domain.Entities;

[Table("users")]
public class UserEntity
{
    public UserEntity() { }

    public long id { get; init; }
    public string name { get; init; } = string.Empty;
    public string email { get; init; } = string.Empty;
    public string password { get; init; } = string.Empty;
    public DateTime created_at { get; init; } = DateTime.UtcNow;
}
