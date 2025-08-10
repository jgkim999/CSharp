using Demo.Application.DTO.User;
using FluentResults;

namespace Demo.Application.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Asynchronously creates a new user with the specified name, email, and SHA-256 hashed password.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordSha256">The user's password hashed using SHA-256.</param>
    /// <returns>A task that represents the asynchronous operation, containing a result indicating success or failure.</returns>
    Task<Result> CreateAsync(string name, string email, string passwordSha256);
    
    /// <summary>
    /// Asynchronously retrieves all users as data transfer objects.
    /// </summary>
    /// <returns>A task that resolves to a result containing a collection of user DTOs.</returns>
    Task<Result<IEnumerable<UserDto>>> GetAllAsync(int limit = 10);
}
