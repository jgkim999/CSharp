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

    public async Task StringSetExpireAsync(RedisKey key, RedisValue value, TimeSpan ts)
    {
        var db = GetDatabase();
        await db.StringSetAsync(key, value);
        await db.KeyExpireAsync(key, ts);
    }

    public async Task<string> GetStringAsync(RedisKey key)
    {
        var db = GetDatabase();
#pragma warning disable CS8603 // 가능한 null 참조 반환입니다.
        return await db.StringGetAsync(key);
#pragma warning restore CS8603 // 가능한 null 참조 반환입니다.
    }

    public async Task<RedisValue> GetAsync(RedisKey key)
    {
        var db = GetDatabase();
        return await db.StringGetAsync(key);
    }
}
