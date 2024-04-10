using WebApiDomain.Models;

namespace WebApiApplication.Interfaces;

public interface IGameService
{
    Task AddItemAsync(long userId, int count);
    Task AddOneItemAsync(long userId);
    Task<IEnumerable<GameItem>> GetAllItemAsync(long userId);
}
