using CacheSample.Application.Abstractions.Caching;
using LanguageExt;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CacheSample.Infrastructure.Caching;

public class CacheService : ICacheService
{
    private static readonly ConcurrentDictionary<string, bool> CacheKeys = new();
    private readonly IDistributedCache _distributedCache;

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<Option<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class
    {
        string? cacheValue = await _distributedCache.GetStringAsync(key, cancellationToken);
        if (cacheValue is null)
            return null;
        T? value = JsonSerializer.Deserialize<T>(cacheValue);
        return value;
    }

    public async Task<Option<T>> GetAsync<T>(string key, Func<Task<T>> factory, int absoluteExpirationRelativeToNow = 60, CancellationToken cancellationToken = default)
        where T : class
    {
        var cacheValue = await GetAsync<T>(key, cancellationToken);
        if (cacheValue.IsSome)
            return cacheValue;
        cacheValue = await factory();
        if (cacheValue.IsSome)
        {
            await SetAsync(
                key,
                cacheValue,
                new DistributedCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(absoluteExpirationRelativeToNow)
                },
                cancellationToken);
        }
        return cacheValue;
    }

    public async Task SetAsync<T>(string key, Option<T> value, DistributedCacheEntryOptions option, CancellationToken cancellationToken = default) where T : class
    {
        await value.IfSomeAsync(
            item =>
            {
                string jsonString = JsonSerializer.Serialize(item);
                _distributedCache.SetStringAsync(key, jsonString, option, cancellationToken);
                CacheKeys.TryAdd(key, false);
            });
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