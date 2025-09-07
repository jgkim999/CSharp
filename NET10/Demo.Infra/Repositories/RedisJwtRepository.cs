using Demo.Application.Configs;
using Demo.Domain.Repositories;
using FastEndpoints.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using StackExchange.Redis;
using System.Diagnostics;
using Polly;

namespace Demo.Infra.Repositories;

/// <summary>
/// Redis-based JWT token repository for storing and validating refresh tokens
/// </summary>
public class RedisJwtRepository : IJwtRepository
{
    private readonly ILogger<RedisJwtRepository> _logger;
    private static ConnectionMultiplexer? _multiplexer;
    private static string? _keyPrefix;
    private static IDatabase? _database;
    private readonly IAsyncPolicy _policyWrap;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisJwtRepository"/> class, establishing a Redis connection for JWT token storage and validation.
    /// </summary>
    /// <param name="logger">Logger for recording operational events and errors.</param>
    /// <param name="redisConfig">Redis configuration options. Must not be null.</param>
    /// <param name="redisInstrumentation">Optional telemetry instrumentation for Redis connections.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="redisConfig"/> is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the Redis connection test fails.</exception>
    public RedisJwtRepository(
        ILogger<RedisJwtRepository> logger,
        IOptions<RedisConfig>? redisConfig,
        StackExchangeRedisInstrumentation? redisInstrumentation)
    {
        _logger = logger;
        
        var retryPolicy = Policy
            .Handle<RedisConnectionException>()
            .RetryAsync(3);
        
        var exponentialBackoffPolicy = Policy
            .Handle<RedisTimeoutException>()
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        
        _policyWrap = Policy.WrapAsync(retryPolicy, exponentialBackoffPolicy);

        try
        {
            if (_multiplexer != null)
                return;
            
            if (redisConfig is null)
                throw new ArgumentNullException();
            
            _multiplexer = ConnectionMultiplexer.Connect(redisConfig.Value.JwtConnectionString);
            redisInstrumentation?.AddConnection(_multiplexer);
            
            _keyPrefix = redisConfig.Value.KeyPrefix;
            _database = _multiplexer.GetDatabase();
            
            var faker = new Bogus.Faker();
            var key = MakeKey(faker.Random.Uuid().ToString());

            _database.StringSet(key, key, TimeSpan.FromDays(1));
            var ret = _database.StringGetDelete(key);
            if (ret != key)
            {
                _multiplexer = null;
                throw new InvalidDataException();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init error");
            throw;
        }
    }

    /// <summary>
    /// Creates a Redis key for JWT token storage
    /// </summary>
    /// <param name="key">User ID or token identifier</param>
    /// <returns>Formatted Redis key</returns>
    private static string MakeKey(string key)
    {
        return string.IsNullOrEmpty(_keyPrefix) ?
            $"jwt:token:{key}" :
            $"{_keyPrefix}:jwt:refreshToken:{key}";
    }

    /// <summary>
    /// Stores a refresh token in Redis for the specified user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="refreshToken">Refresh token to store</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task StoreTokenAsync(string userId, string refreshToken)
    {
        try
        {
            using var activity = Activity.Current?.Source.StartActivity("StoreTokenAsync");
            if (_database is null)
                throw new NullReferenceException();
            await _policyWrap.ExecuteAsync(() => 
                _database.StringSetAsync(MakeKey(userId), refreshToken, TimeSpan.FromDays(1)));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Redis");
            throw;
        }
    }

    /// <summary>
    /// Stores a refresh token in Redis for the specified user (backward compatibility)
    /// </summary>
    /// <param name="response">Token response containing user ID and refresh token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public async Task StoreTokenAsync(TokenResponse response)
    {
        await StoreTokenAsync(response.UserId, response.RefreshToken);
    }

    /// <summary>
    /// Validates if the provided refresh token matches the stored token for the user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="refreshToken">Refresh token to validate</param>
    /// <returns>True if token is valid, false otherwise</returns>
    public async Task<bool> TokenIsValidAsync(string userId, string refreshToken)
    {
        try
        {
            using var activity = Activity.Current?.Source.StartActivity("TokenIsValidAsync");
            if (_database is null)
                throw new NullReferenceException();
            var token = await _policyWrap.ExecuteAsync(() => _database.StringGetAsync(MakeKey(userId)));
            return token == refreshToken;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Redis");
            throw;
        }
    }
}
