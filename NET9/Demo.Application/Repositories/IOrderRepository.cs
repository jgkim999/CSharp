using FluentResults;

namespace Demo.Application.Repositories;

public interface IOrderRepository
{
    Task<Result<int>> GetOrderCount(string state);
}
