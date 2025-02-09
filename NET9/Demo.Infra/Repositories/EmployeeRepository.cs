using Dapper;

using Demo.Application.Repositories;
using Demo.Domain.Models;

using FluentResults;

using Microsoft.Extensions.Logging;

using MySqlConnector;

namespace Demo.Infra.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ILogger<EmployeeRepository> _logger;
    private readonly string _connectionString;

    public EmployeeRepository(string connectionString, ILogger<EmployeeRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<Employee>>> GetAllAsync()
    {
        try
        {
            MySqlConnection conn = new(_connectionString);
            await conn.OpenAsync();

            string sql = "SELECT A.*, B.*, C.*" +
                         " FROM employees AS A" +
                         " INNER JOIN offices AS B ON A.officeCode = B.officeCode" +
                         " LEFT JOIN employees AS C ON A.reportsTo = C.employeeNumber;";

            var employees = await conn.QueryAsync<Employee, Office, Employee, Employee>(sql, (employee, office, manager) =>
            {
                employee.Office = office;
                employee.Manager = manager;
                return employee;
            },
            splitOn: "officeCode, employeeNumber");
            return Result.Ok(employees);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Result.Fail<IEnumerable<Employee>>(e.Message);
        }
    }

    public async Task<Result<Employee?>> GetByIdAsync(int employeeNumber)
    {
        throw new NotImplementedException();
    }

    public async Task AddAsync(Employee employee)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(Employee employee)
    {
        throw new NotImplementedException();
    }

    public async Task DeleteAsync(int employeeNumber)
    {
        throw new NotImplementedException();
    }
}
