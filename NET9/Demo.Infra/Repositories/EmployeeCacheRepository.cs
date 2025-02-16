using Demo.Application.Repositories;
using Demo.Domain.Models;
using Demo.Infra.Extentions;

using FluentResults;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Demo.Infra.Repositories;

class EmployeeCacheRepository : IEmployeeCacheRepository
{
    private readonly ILogger<EmployeeCacheRepository> _logger;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _multiplexer;

    public EmployeeCacheRepository(
        ILogger<EmployeeCacheRepository> logger,
        IEmployeeRepository employeeRepository,
        IDistributedCache cache,
        IConnectionMultiplexer multiplexer)
    {
        _logger = logger;
        _employeeRepository = employeeRepository;
        _cache = cache;
        _multiplexer = multiplexer;
    }

    public async Task<Result<IEnumerable<Employee>>> GetAllAsync()
    {
        IDatabase db = _multiplexer.GetDatabase();
        var result = await db.GetAsync<List<Employee>>("employee-all");
        if (result.IsSuccess)
            return result.Value;

        var dbRes = await _employeeRepository.GetAllAsync();
        if (dbRes.IsSuccess)
            await db.SetJsonAsync("employee-all", dbRes.Value);
        return dbRes;
    }

    public Task<Result<Employee?>> GetByIdAsync(int employeeNumber)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Employee employee)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Employee employee)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAsync(int employeeNumber)
    {
        throw new NotImplementedException();
    }
}