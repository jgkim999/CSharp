using System.Diagnostics;
using Demo.Application.Configs;
using Demo.Domain.Repositories;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using Polly;
using StackExchange.Redis;

namespace Demo.Infra.Repositories;

/// <summary>
/// Redis cache implementation for IP to nation mapping
/// </summary>
public class IpToNationRedisCache : IIpToNationCache
{
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly string? _keyPrefix;
    private readonly IDatabase? _database;
    private readonly IAsyncPolicy _policyWrap;

    /// <summary>
    /// Initializes a new instance of the IpToNationRedisCache class
    /// </summary>
    /// <param name="config">Redis configuration options</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="redisInstrumentation">Redis instrumentation for telemetry</param>
    /// <exception cref="NullReferenceException">Thrown when database connection fails</exception>
    public IpToNationRedisCache(
        IOptions<RedisConfig> config,
        ILogger<IpToNationRedisCache> logger,
        StackExchangeRedisInstrumentation redisInstrumentation)
    {
        try
        {
            _multiplexer = ConnectionMultiplexer.Connect(config.Value.IpToNationConnectionString);
            redisInstrumentation.AddConnection(_multiplexer);
            _keyPrefix = config.Value.KeyPrefix;
            _database = _multiplexer.GetDatabase();
            if (_database is null)
            {
                throw new NullReferenceException("_database is null");
            }
            
            _policyWrap = Policy
                .Handle<RedisConnectionException>()
                .RetryAsync(3);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Connection failed");
            throw;
        }
    }

    /// <summary>
    /// Creates a cache key for the given client IP
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <returns>Formatted cache key</returns>
    private string MakeKey(string clientIp)
    {
        return string.IsNullOrEmpty(_keyPrefix) ?
            $"ipcache:{clientIp}" :
            $"{_keyPrefix}:ipcache:{clientIp}";
    }

    /// <summary>
    /// Asynchronously retrieves the cached country code for the specified client IP address.
    /// </summary>
    /// <param name="clientIp">The IP address to look up in the cache.</param>
    /// <returns>A Result containing the country code if found; otherwise, a failure Result with the message "Not found".</returns>
    public async Task<Result<string>> GetAsync(string clientIp)
    {
        Debug.Assert(_database != null, nameof(_database) + " != null");
        
        var result = await _policyWrap.ExecuteAsync(() => _database.StringGetAsync(MakeKey(clientIp)));
        
        return result.IsNull ? Result.Fail("Not found") : Result.Ok(result.ToString());
    }

    /// <summary>
    /// Sets the country code for the specified IP address in cache
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <param name="countryCode">Country code to cache</param>
    /// <param name="ts">Cache expiration time</param>
    public async Task SetAsync(string clientIp, string countryCode, TimeSpan ts)
    {
        Debug.Assert(_database != null, nameof(_database) + " != null");
        await _policyWrap.ExecuteAsync(() => _database.StringSetAsync(MakeKey(clientIp), countryCode, ts));
    }
}
