using Demo.Application.DTO.User;
using FluentResults;

namespace Demo.Application.Repositories;

public interface IUserRepository
{
    Task<Result> CreateAsync(string name, string email, string passwordSha256);
    Task<Result<IEnumerable<UserDto>>> GetAllAsync();
}
