using Demo.Application.Repositories;
using Demo.Infra.Repositories;

using Microsoft.Extensions.Logging;

using Moq;

namespace Demo.xUnitTest;

public class UnitTest1
{
    string connectionString = "Server=192.168.0.47;User ID=test;Password=1234;Database=classicmodels;";

    [Fact]
    public async Task OrderRepository()
    {
        OrderRepository orderRepository = new OrderRepository(connectionString);
        var count = await orderRepository.GetOrderCount("Shipped");
        Assert.NotEqual(0, count);
    }

    [Fact]
    public async Task EmployeeRepository()
    {
        var logger = Mock.Of<ILogger<EmployeeRepository>>();

        IEmployeeRepository employeeRepo = new EmployeeRepository(connectionString, logger);
        var employees = await employeeRepo.GetAllAsync();
        Assert.NotNull(employees);
    }
}
