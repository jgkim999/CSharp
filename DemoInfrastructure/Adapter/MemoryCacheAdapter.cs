using DemoApplication.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DemoInfrastructure.Adapter;

public class MemoryCacheAdapter : ICache
{
    private readonly MemoryCache _cache;

    public MemoryCacheAdapter()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
    }
    
    public T? Get<T>(string key)
    {
        return _cache.Get<T>(key);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        await Task.CompletedTask;
        return Get<T>(key);
    }

    public void Set<T>(string key, T value, TimeSpan expirationTime)
    {
        _cache.Set(key, value, expirationTime);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expirationTime)
    {
        await Task.CompletedTask;
        Set(key, value, expirationTime);
    }

    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    public async Task RemoveAsync(string key)
    {
        await Task.CompletedTask;
        Remove(key);
    }
}