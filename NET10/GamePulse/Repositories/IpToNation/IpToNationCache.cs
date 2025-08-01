using System.Diagnostics;
using GamePulse.Configs;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;
using FluentResults;

namespace GamePulse.Repositories.IpToNation;

/// <summary>
/// Redis cache implementation for IP to nation mapping
/// </summary>
public class IpToNationCache : IIpToNationCache
{
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly string? _keyPrefix;
    private readonly IDatabase? _database;
    
    /// <summary>
    /// Initializes a new instance of the IpToNationCache class
    /// </summary>
    /// <param name="config">Redis configuration options</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="redisInstrumentation">Redis instrumentation for telemetry</param>
    /// <exception cref="NullReferenceException">Thrown when database connection fails</exception>
    public IpToNationCache(
        IOptions<RedisConfig> config,
        ILogger<IpToNationCache> logger,
        StackExchangeRedisInstrumentation redisInstrumentation)
    {
        try
        {
            _multiplexer = ConnectionMultiplexer.Connect(config.Value.IpToNationConnectionString);
            redisInstrumentation.AddConnection(_multiplexer);
            _keyPrefix = config.Value.KeyPrefix;
            _database = _multiplexer.GetDatabase();
            if (_database is null)
                throw new NullReferenceException();
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
    /// Gets the country code for the specified IP address from cache
    /// </summary>
    /// <param name="clientIp">Client IP address</param>
    /// <returns>Result containing country code or failure</returns>
    public async Task<Result<string>> GetAsync(string clientIp)
    {
        Debug.Assert(_database != null, nameof(_database) + " != null");
        var result = await _database.StringGetAsync(MakeKey(clientIp));
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
        await _database.StringSetAsync(MakeKey(clientIp), countryCode, ts);
    }
}
