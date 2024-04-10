using Microsoft.Extensions.Logging;
using WebApiApplication.Interfaces;

namespace WebApiInfrastructure.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly ILogger<SessionRepository> _logger;
    private readonly IRedisManager _redisManager;

    public SessionRepository(ILogger<SessionRepository> logger, IRedisManager redisManager)
    {
        _logger = logger;
        _redisManager = redisManager;
    }

    public async Task<string> GetAsync(string key)
    {
        return await _redisManager.GetStringAsync(key);
    }

    public async Task SetAsync(string key, string sessionId)
    {
        await _redisManager.StringSetExpireAsync(key, sessionId, TimeSpan.FromHours(1));
    }
}
