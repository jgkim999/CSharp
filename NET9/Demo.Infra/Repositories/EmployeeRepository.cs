using Dapper;

using Demo.Application.Repositories;
using Demo.Domain.Models;
using Demo.Infra.Config;

using FluentResults;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MySqlConnector;

namespace Demo.Infra.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ILogger<EmployeeRepository> _logger;
    private readonly MySqlConfig _mySqlConfig;

    public EmployeeRepository(IOptions<MySqlConfig> mySqlConfig, ILogger<EmployeeRepository> logger)
    {
        _mySqlConfig = mySqlConfig.Value;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<Employee>>> GetAllAsync()
    {
        try
        {
            await using MySqlConnection conn = new(_mySqlConfig.ConnectionString);
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
        try
        {
            string sql = "SELECT A.*, B.*, C.*" +
                         " FROM employees AS A" +
                         " INNER JOIN offices AS B ON A.officeCode = B.officeCode" +
                         " LEFT JOIN employees AS C ON A.reportsTo = C.employeeNumber" +
                         " WHERE A.employeeNumber = @employeeNumber;";
            await using MySqlConnection conn = new(_mySqlConfig.ConnectionString);
            await conn.OpenAsync();

            DynamicParameters parameters = new();
            parameters.Add("employeeNumber", employeeNumber);

            var employees = await conn.QueryAsync<Employee, Office, Employee, Employee>(sql, (employee, office, manager) =>
                {
                    employee.Office = office;
                    employee.Manager = manager;
                    return employee;
                },
                splitOn: "officeCode, employeeNumber",
                param: parameters);
            return Result.Ok(employees?.First());
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Result.Fail(e.Message);
        }
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
