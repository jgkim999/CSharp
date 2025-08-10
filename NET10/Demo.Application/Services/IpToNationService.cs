using Demo.Application.Repositories;

namespace Demo.Application.Services;

public class IpToNationService : IIpToNationService
{
    private readonly IIpToNationCache _cache;
    private readonly IIpToNationRepository _repo;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpToNationService"/> class with the specified cache, repository, and telemetry service dependencies.
    /// </summary>
    public IpToNationService(IIpToNationCache cache, IIpToNationRepository repo, ITelemetryService telemetryService)
    {
        _cache = cache;
        _repo = repo;
        _telemetryService = telemetryService;
    }

    /// <summary>
    /// Asynchronously retrieves the nation code associated with the specified client IP address.
    /// </summary>
    /// <param name="clientIp">The IP address of the client to resolve.</param>
    /// <param name="ct">A cancellation token for the asynchronous operation.</param>
    /// <returns>The nation code corresponding to the provided client IP address.</returns>
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
