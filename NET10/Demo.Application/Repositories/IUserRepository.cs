using Demo.Application.DTO;
using FluentResults;

namespace Demo.Application.Repositories;

public interface IUserRepository
{
    Task<Result<IEnumerable<UserDto>>> GetAllAsync();
}
