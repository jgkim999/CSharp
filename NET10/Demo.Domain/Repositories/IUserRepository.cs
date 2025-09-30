using Demo.Domain.Entities;
using FluentResults;

namespace Demo.Domain.Repositories;

public interface IUserRepository
{
    /// <summary>
    /// Asynchronously creates a new user with the specified name, email, and SHA-256 hashed password.
    /// </summary>
    /// <param name="name">The user's name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="passwordSha256">The user's password hashed using SHA-256.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation, containing a result with the created user entity or failure.</returns>
    Task<Result<UserEntity>> CreateAsync(string name, string email, string passwordSha256, CancellationToken ct = default);
    
    /// <summary>
    /// Asynchronously retrieves users with pagination and optional search functionality.
    /// </summary>
    /// <param name="searchTerm">Optional search term to filter users by name.</param>
    /// <param name="page">Page number (0-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that resolves to a result containing a tuple of users and total count.</returns>
    Task<Result<(IEnumerable<UserEntity> Users, int TotalCount)>> GetPagedAsync(string? searchTerm, int page, int pageSize, CancellationToken ct = default);

    Task<Result<UserEntity?>> FindByIdAsync(long id, CancellationToken cancellationToken);
}
