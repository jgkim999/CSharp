using Demo.Domain.Repositories;
using IP2Location;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Demo.Infra.Repositories;

/// <summary>
/// Repository for IP to nation lookup using IP2Location database
/// </summary>
public class IpToNationRepository : IIpToNationRepository
{
    private readonly Component _component = new ();
    
    /// <summary>
    /// Initializes a new instance of the IpToNationRepository class
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="hostingEnvironment">Web host environment for file path resolution</param>
    public IpToNationRepository(ILogger<IpToNationRepository> logger, IWebHostEnvironment hostingEnvironment)
    {
        var path = Path.Combine(hostingEnvironment.ContentRootPath, "IP2LOCATION-LITE-DB3.BIN");
        try
        {
            _component.Open(path);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Read file {Path}", path);
            throw;
        }
    }

    /// <summary>
    /// Gets the country code for the specified IP address
    /// </summary>
    /// <param name="clientIp">Client IP address to lookup</param>
    /// <returns>Country code or "unknown" if lookup fails</returns>
    public async Task<string> GetAsync(string clientIp)
    {
        var result = await _component.IPQueryAsync(clientIp);
        return result.Status == "OK" ? result.CountryShort : "unknown";
    }
}
