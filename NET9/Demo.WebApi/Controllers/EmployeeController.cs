using Demo.Application.Repositories;
using Demo.Domain.Models;

using Microsoft.AspNetCore.Mvc;

namespace Demo.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class EmployeeController : ControllerBase
{
    private readonly ILogger<EmployeeController> _logger;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEmployeeCacheRepository _employeeCacheRepository;

    public EmployeeController(
        ILogger<EmployeeController> logger,
        IEmployeeRepository employeeRepository,
        IEmployeeCacheRepository employeeCacheRepository)
    {
        _logger = logger;
        _employeeRepository = employeeRepository;
        _employeeCacheRepository = employeeCacheRepository;
    }

    [HttpGet(Name = "Get")]
    public async Task<IEnumerable<Employee>> Get()
    {
        var res = await _employeeRepository.GetAllAsync();
        return res.Value;
    }

    [HttpGet(Name = "GetCache")]
    public async Task<IEnumerable<Employee>> GetCache()
    {
        var res = await _employeeCacheRepository.GetAllAsync();
        return res.Value;
    }
}
