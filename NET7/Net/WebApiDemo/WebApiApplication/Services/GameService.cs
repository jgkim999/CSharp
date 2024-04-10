using Bogus;
using Microsoft.Extensions.Logging;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiApplication.Services;

public class GameService : IGameService
{
    private readonly ILogger<GameService> _logger;
    private readonly IGameRepository _gameRepo;

    public GameService(ILogger<GameService> logger, IGameRepository gameItemRepo)
    {
        _logger = logger;
        _gameRepo = gameItemRepo;
    }

    public async Task<IGameUser> GetAsync(long userId)
    {
        IGameUser user = new GameUser(userId);
        var items = await _gameRepo.GetAllItemAsync(userId);
        user.SetItems(items);
        return user;
    }

    public async Task AddOneItemAsync(long userId)
    {
        Faker faker = new Faker();
        GameItem item = new GameItem()
        {
            AccountId = userId,
            ItemId = faker.Random.Int(1, int.MaxValue),
            Amount = faker.Random.Int(1, 100)
        };
        await _gameRepo.AddItemAsync(item);
    }

    public async Task AddItemAsync(long userId, int count)
    {
        Faker faker = new Faker();
        var items = new List<GameItem>();
        for (int i = 0; i < count; ++i)
        {
            items.Add(
                new GameItem()
                {
                    AccountId = userId,
                    ItemId = faker.Random.Int(1, int.MaxValue),
                    Amount = faker.Random.Int(1, 100)
                });
        }
        await _gameRepo.AddAsync(items);
    }

    public async Task<IEnumerable<GameItem>> GetAllItemAsync(long userId)
    {
        return await _gameRepo.GetAllItemAsync(userId);
    }
}
