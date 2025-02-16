using Demo.Domain.Models;

using FluentResults;

namespace Demo.Application.Repositories;

public interface IEmployeeCacheRepository
{
    Task<Result<IEnumerable<Employee>>> GetAllAsync();
    Task<Result<Employee?>> GetByIdAsync(int employeeNumber);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(int employeeNumber);
}
