using Demo.Application.Repositories;
using Demo.Infra.Config;
using Demo.Infra.Repositories;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

namespace Demo.xUnitTest;

public class EmployeeRepositoryTest : IDisposable
{
    private Mock<IOptions<MySqlConfig>> _dbMock;
    private ILogger<EmployeeRepository> _logger;

    public EmployeeRepositoryTest()
    {
        MySqlConfig mysqlConfig = new()
        {
            ConnectionString = "Server=192.168.0.47;User ID=test;Password=1234;Database=classicmodels;"
        };
        _dbMock = new();
        _dbMock.Setup(ap => ap.Value).Returns(mysqlConfig);

        _logger = Mock.Of<ILogger<EmployeeRepository>>();
    }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }
    
    [Fact]
    public async Task EmployeeRepositoryGetAllAsync()
    {
        IEmployeeRepository employeeRepo = new EmployeeRepository(_dbMock.Object, _logger);
        var result = await employeeRepo.GetAllAsync();
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task EmployeeRepositoryGetByIdAsync()
    {
        IEmployeeRepository employeeRepo = new EmployeeRepository(_dbMock.Object, _logger);
        var result = await employeeRepo.GetByIdAsync(1002);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task EmployeeRepositoryGetByIdAsync2()
    {
        IEmployeeRepository employeeRepo = new EmployeeRepository(_dbMock.Object, _logger);
        var result = await employeeRepo.GetByIdAsync(10002);
        Assert.True(result.IsFailed);
    }
}
