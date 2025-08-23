using Demo.Application.Configs;
using Demo.Domain.Repositories;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Demo.Infra.Tests.TestHelpers;

/// <summary>
/// 테스트용 IpToNationRedisCache 구현체
/// OpenTelemetry instrumentation 의존성을 제거하여 테스트에서 사용
/// </summary>
public class TestableIpToNationRedisCache : IIpToNationCache
{
    private readonly ConnectionMultiplexer _multiplexer;
    private readonly string? _keyPrefix;
    private readonly IDatabase? _database;

    public TestableIpToNationRedisCache(
        IOptions<RedisConfig> config,
        ILogger<TestableIpToNationRedisCache> logger)
    {
        try
        {
            _multiplexer = ConnectionMultiplexer.Connect(config.Value.IpToNationConnectionString);
            _keyPrefix = config.Value.KeyPrefix;
            _database = _multiplexer.GetDatabase();
            if (_database is null)
            {
                throw new NullReferenceException("_database is null");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Connection failed");
            throw;
        }
    }

    private string MakeKey(string clientIp)
    {
        return string.IsNullOrEmpty(_keyPrefix) ?
            $"ipcache:{clientIp}" :
            $"{_keyPrefix}:ipcache:{clientIp}";
    }

    public async Task<Result<string>> GetAsync(string clientIp)
    {
        System.Diagnostics.Debug.Assert(_database != null, nameof(_database) + " != null");
        var result = await _database.StringGetAsync(MakeKey(clientIp));
        return result.IsNull ? Result.Fail("Not found") : Result.Ok(result.ToString());
    }

    public async Task SetAsync(string clientIp, string countryCode, TimeSpan ts)
    {
        System.Diagnostics.Debug.Assert(_database != null, nameof(_database) + " != null");
        await _database.StringSetAsync(MakeKey(clientIp), countryCode, ts);
    }
}