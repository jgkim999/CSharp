using Demo.Infra.Repositories;

namespace Demo.xUnitTest;

public class UnitTest1
{
    [Fact]
    public async Task OrderRepository()
    {
        string connectionString = "Server=192.168.0.47;User ID=test;Password=1234;Database=classicmodels;";

        OrderRepository orderRepository = new OrderRepository(connectionString);
        var count = await orderRepository.GetOrderCount("Shipped");
        Assert.NotEqual(0, count);
    }
}
