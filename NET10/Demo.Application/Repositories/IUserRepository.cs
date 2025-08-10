using Demo.Application.DTO.User;
using FluentResults;

namespace Demo.Application.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Asynchronously creates a new user with the specified name, email, and SHA-256 hashed password.
    /// </summary>
    /// <param name="name">The name of the user to create.</param>
    /// <param name="email">The email address of the user to create.</param>
    /// <param name="passwordSha256">The SHA-256 hash of the user's password.</param>
    /// <returns>A task representing the asynchronous operation, containing a result that indicates whether the user was successfully created.</returns>
    Task<Result> CreateAsync(string name, string email, string passwordSha256);
    
    /// <summary>
    /// Asynchronously retrieves a collection of user data transfer objects, limited to the specified number.
    /// </summary>
    /// <param name="limit">The maximum number of users to return. Defaults to 10.</param>
    /// <returns>A task that resolves to a result containing the retrieved user DTOs.</returns>
    Task<Result<IEnumerable<UserDto>>> GetAllAsync(int limit = 10);
}
