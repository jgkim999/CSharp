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

    public EmployeeController(
        ILogger<EmployeeController> logger,
        IEmployeeRepository employeeRepository)
    {
        _logger = logger;
        _employeeRepository = employeeRepository;
    }

    [HttpGet(Name = "Get")]
    public async Task<IEnumerable<Employee>> Get()
    {
        var res = await _employeeRepository.GetAllAsync();
        return res.Value;
    }
}
