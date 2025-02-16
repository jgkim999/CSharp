using Demo.Infra.Config;
using Demo.Infra.Repositories;
using Microsoft.Extensions.Options;
using Moq;

namespace Demo.xUnitTest;

public class OrderRepositoryTest : IDisposable
{
    private Mock<IOptions<MySqlConfig>> _dbMock;

    public OrderRepositoryTest()
    {
        MySqlConfig mysqlConfig = new()
        {
            ConnectionString = "Server=192.168.0.47;User ID=test;Password=1234;Database=classicmodels;"
        };
        _dbMock = new();
        _dbMock.Setup(ap => ap.Value).Returns(mysqlConfig);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    [Fact]
    public async Task OrderRepository()
    {
        OrderRepository orderRepository = new OrderRepository(_dbMock.Object);
        var count = await orderRepository.GetOrderCount("Shipped");
        Assert.NotEqual(0, count);
    }
}
