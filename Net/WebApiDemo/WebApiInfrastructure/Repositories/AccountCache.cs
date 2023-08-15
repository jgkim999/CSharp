using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebApiApplication.Interfaces;
using WebApiDomain.Models;

namespace WebApiInfrastructure.Repositories;

public class AccountCache : IAccountCache
{
    private readonly ILogger<AccountCache> _logger;
    private readonly IRedisManager _redisManager;
    private readonly string _accountPrefix = "user:";

    public AccountCache(ILogger<AccountCache> logger, IRedisManager redisManager)
    {
        _logger = logger;
        _redisManager = redisManager;
    }

    public async Task SetAsync(AccountDto dto)
    {
        try
        {
            await _redisManager.SetAddKeyExpireAsync(_accountPrefix + dto.Ulid, JsonConvert.SerializeObject(dto), TimeSpan.FromHours(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
            throw;
        }
    }
}
