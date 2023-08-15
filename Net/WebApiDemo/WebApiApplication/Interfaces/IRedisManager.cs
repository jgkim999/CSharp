using StackExchange.Redis;

namespace WebApiApplication.Interfaces;

public interface IRedisManager
{
    IDatabase GetDatabase();
    Task SetAddKeyExpireAsync(RedisKey key, RedisValue value, TimeSpan ts);
}
