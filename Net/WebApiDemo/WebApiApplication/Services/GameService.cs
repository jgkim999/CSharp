using Bogus;
using Microsoft.Extensions.Logging;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiApplication.Services;

public class GameService : IGameService
{
    private readonly ILogger<GameService> _logger;
    private readonly IGameItemRepository _gameItemRepo;

    public GameService(ILogger<GameService> logger, IGameItemRepository gameItemRepo)
    {
        _logger = logger;
        _gameItemRepo = gameItemRepo;
    }

    public async Task AddOneAsync(long userId)
    {
        Faker faker = new Faker();
        GameItem item = new GameItem()
        {
            AccountId = userId,
            ItemId = faker.Random.Int(1, int.MaxValue),
            Amount = faker.Random.Int(1, 100)
        };
        await _gameItemRepo.AddAsync(item);
    }

    public async Task AddAsync(long userId, int count)
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
        await _gameItemRepo.AddAsync(items);
    }
}
