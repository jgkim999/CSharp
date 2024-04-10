using StackExchange.Redis;

namespace WebApiApplication.Interfaces;

public interface IRedisManager
{
    IDatabase GetDatabase();
    Task StringSetExpireAsync(RedisKey key, RedisValue value, TimeSpan ts);
    Task<RedisValue> GetAsync(RedisKey key);
    Task<string> GetStringAsync(RedisKey key);
}
