using System.Text.Json;
using FastEndpoints.Security;
using GamePulse.Configs;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Trace;
using StackExchange.Redis;

namespace GamePulse.Repositories.Jwt;
/// <summary>
/// 
/// </summary>
public class RedisJwtRepository : IJwtRepository
{
    private readonly ILogger<RedisJwtRepository> _logger;
    private static ConnectionMultiplexer? _multiplexer;
    private static string? _keyPrefix;
    private static IDatabase? _database;
    private readonly Tracer _tracer;
    private readonly StackExchangeRedisInstrumentation _redisInstrumentation;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    /// <param name="tracer"></param>
    /// <param name="redisInstrumentation"></param>
    /// <exception cref="Exception"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public RedisJwtRepository(IConfiguration config, ILogger<RedisJwtRepository> logger, Tracer tracer, StackExchangeRedisInstrumentation redisInstrumentation)
    {
        _redisInstrumentation = redisInstrumentation;
        _tracer = tracer;
        _logger = logger;
        var jwtConfig = config.GetSection("Jwt").Get<JwtConfig>();
        if (jwtConfig == null)
        {
            throw new Exception("Jwt config not found");
        }
        try
        {
            if (_multiplexer != null)
                return;
            _multiplexer = ConnectionMultiplexer.Connect(jwtConfig.RedisConnectionString);
            _redisInstrumentation.AddConnection(_multiplexer);
            _keyPrefix = jwtConfig.KeyPrefix;
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
    
    private static string MakeKey(string key)
    {
        return string.IsNullOrEmpty(_keyPrefix) ? 
            $"jwt:token:{key}" :
            $"{_keyPrefix}:jwt:refreshToken:{key}";
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task StoreTokenAsync(TokenResponse response)
    {
        try
        {
            using var span = _tracer.StartActiveSpan("StoreTokenAsync");
            if (_database is null)
                throw new NullReferenceException();
            await _database.StringSetAsync(MakeKey(response.UserId), response.RefreshToken, TimeSpan.FromDays(1));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Redis");
            throw;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="refreshToken"></param>
    /// <returns></returns>
    public async Task<bool> TokenIsValidAsync(string userId, string refreshToken)
    {
        try
        {
            using var span = _tracer.StartActiveSpan("TokenIsValidAsync");
            if (_database is null)
                throw new NullReferenceException();
            var token = await _database.StringGetAsync(MakeKey(userId));
            return token == refreshToken;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Redis");
            throw;
        }
    }
}
