namespace WebApiApplication.Interfaces;

public interface IGameService
{
    Task AddAsync(long userId, int count);
    Task AddOneAsync(long userId);
}
