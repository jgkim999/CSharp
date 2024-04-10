using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IGameRepository
{
    Task<int> AddItemAsync(GameItem item);
    Task<int> AddAsync(List<GameItem> items);
    Task<IEnumerable<GameItem>> GetAllItemAsync(long userId);
}
