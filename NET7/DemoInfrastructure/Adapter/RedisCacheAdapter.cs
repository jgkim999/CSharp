using System.Security.Cryptography.X509Certificates;
using DemoApplication.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace DemoInfrastructure.Adapter;

public class RedisCacheAdapter : ICache
{
    private readonly ConnectionMultiplexer _redis;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration">"mycache.redis.cache.windows.net,abortConnect=false,ssl=true,password=..."</param>
    public RedisCacheAdapter(string configuration)
    {
        _redis = ConnectionMultiplexer.Connect(configuration);
    }
    
    public T? Get<T>(string key)
    {
        var db = _redis.GetDatabase(0);
        string? json = db.StringGet(key);
        return json is not null ? JsonConvert.DeserializeObject<T>(json) : default (T);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var db = _redis.GetDatabase(0);
        string? json = await db.StringGetAsync(key);
        return json is not null ? JsonConvert.DeserializeObject<T>(json) : default (T);
    }

    public void Set<T>(string key, T value, TimeSpan expirationTime)
    {
        var db = _redis.GetDatabase(0);
        var json = JsonConvert.SerializeObject(value);
        db.StringSet(key, json, expirationTime);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expirationTime)
    {
        var db = _redis.GetDatabase(0);
        var json = JsonConvert.SerializeObject(value);
        await db.StringSetAsync(key, json, expirationTime);
    }

    public void Remove(string key)
    {
        var db = _redis.GetDatabase(0);
        db.KeyDelete(key);
    }

    public async Task RemoveAsync(string key)
    {
        var db = _redis.GetDatabase(0);
        await db.KeyDeleteAsync(key);
    }
}