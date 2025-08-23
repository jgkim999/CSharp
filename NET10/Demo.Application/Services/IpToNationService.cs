using Demo.Domain.Repositories;

namespace Demo.Application.Services;

public class IpToNationService : IIpToNationService
{
    private readonly IIpToNationCache _cache;
    private readonly IIpToNationRepository _repo;
    private readonly ITelemetryService _telemetryService;

    public IpToNationService(IIpToNationCache cache, IIpToNationRepository repo, ITelemetryService telemetryService)
    {
        _cache = cache;
        _repo = repo;
        _telemetryService = telemetryService;
    }

    public async Task<string> GetNationCodeAsync(string clientIp, CancellationToken ct)
    {
        using var span = _telemetryService.StartActivity("GetNationCodeAsync");
        /*
        var result = await _cache.GetAsync(clientIp);
        if (result.IsSuccess)
        {
            return result.Value;
        }
        */
        var countryCode = await _repo.GetAsync(clientIp);
        //await _cache.SetAsync(clientIp, countryCode, TimeSpan.FromDays(1));
        return countryCode;
    }
}
