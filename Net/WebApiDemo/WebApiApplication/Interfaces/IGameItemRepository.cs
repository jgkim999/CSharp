using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IGameItemRepository
{
    Task<int> AddAsync(GameItem item);
    Task<int> AddAsync(List<GameItem> items);
}
