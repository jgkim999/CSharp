using LanguageExt;
using Microsoft.Extensions.Caching.Distributed;

namespace CacheSample.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<Option<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        where T : class;

    Task<Option<T>> GetAsync<T>(string key, Func<Task<T>> factory, int absoluteExpirationRelativeToNow = 60, CancellationToken cancellationToken = default)
        where T : class;

    Task SetAsync<T>(string key, Option<T> value, DistributedCacheEntryOptions option, CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default);
}