using Demo.Domain;

namespace Demo.Application;

public interface IUserService
{
    Task<User?> GetUserAsync(string email, CancellationToken ct);
    Task<User?> CreateUserAsync(string email, string password, string salt, DateTime createdAt, DateTime updatedAt, CancellationToken ct);
}
