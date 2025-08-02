using GamePulse.Repositories;
using GamePulse.Repositories.IpToNation.Cache;

namespace GamePulse.Services.IpToNation;

public class IpToNationService : IIpToNationService
{
    private readonly IIpToNationCache _cache;
    private readonly IIpToNationRepository _repo;

    public IpToNationService(IIpToNationCache cache, IIpToNationRepository repo)
    {
        _cache = cache;
        _repo = repo;
    }

    public async Task<string> GetNationCodeAsync(string clientIp, CancellationToken ct)
    {
        using var span = GamePulseActivitySource.StartActivity("GetNationCodeAsync");
        var result = await _cache.GetAsync(clientIp);
        if (result.IsSuccess)
        {
            return result.Value;
        }
        var countryCode = await _repo.GetAsync(clientIp);
        await _cache.SetAsync(clientIp, countryCode, TimeSpan.FromDays(1));
        return countryCode;
    }
}
