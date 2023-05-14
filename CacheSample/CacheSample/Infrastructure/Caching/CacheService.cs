using System.Collections.Concurrent;
using CacheSample.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CacheSample.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private static readonly ConcurrentDictionary<string, bool> CacheKeys = new();
    private readonly IDistributedCache _distributedCache;

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        string? cacheValue = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (cacheValue is null)
            return null;
        T? value = JsonSerializer.Deserialize<T>(cacheValue);
        return value;
    }

    public async Task<T?> GetAsync<T>(string key, Func<Task<T>> factory, CancellationToken cancellationToken = default)
        where T : class
    {
        T? cacheValue = await GetAsync<T>(key, cancellationToken);
        if (cacheValue is not null)
            return cacheValue;
        cacheValue = await factory();
        await SetAsync(key, cacheValue, cancellationToken);
        return cacheValue;
    }

    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default) where T : class
    {
        string jsonString = JsonSerializer.Serialize(value);
        await _distributedCache.SetStringAsync(key, jsonString, cancellationToken);
        CacheKeys.TryAdd(key, false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
        CacheKeys.TryRemove(key, out bool _);
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        IEnumerable<Task> tasks = CacheKeys
            .Keys
            .Where(k => k.StartsWith(prefixKey))
            .Select(k => RemoveAsync(k, cancellationToken));
        await Task.WhenAll(tasks);
    }
}