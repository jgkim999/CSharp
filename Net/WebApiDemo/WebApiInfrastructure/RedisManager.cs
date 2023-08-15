using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WebApiApplication.Interfaces;

namespace WebApiInfrastructure;

public class RedisManager : IRedisManager
{
    private readonly ILogger<RedisManager> _logger;
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisManager(ILogger<RedisManager> logger, IConnectionMultiplexer multiplexer)
    {
        _logger = logger;
        _multiplexer = multiplexer;
    }

    public IDatabase GetDatabase()
    {
        return _multiplexer.GetDatabase(0);
    }

    public async Task SetAddKeyExpireAsync(RedisKey key, RedisValue value, TimeSpan ts)
    {
        var db = GetDatabase();
        await db.SetAddAsync(key, value);
        await db.KeyExpireAsync(key, ts);
    }
}
