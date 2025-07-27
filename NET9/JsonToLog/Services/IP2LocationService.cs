using IP2Country;
using IP2Country.Entities;
using IP2Country.IP2Location.Lite;

namespace JsonToLog.Services;

public class IP2LocationService
{
    private readonly ILogger<IP2LocationService> _logger;
    private readonly IP2CountryResolver _resolver;
    
    public IP2LocationService(ILogger<IP2LocationService> logger)
    {
        _logger = logger;

        try
        {
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "IP2LOCATION-LITE-DB1.CSV");
            _resolver = new IP2CountryResolver(new IP2LocationFileSource(dbPath));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to initialize IP2LocationService. Ensure the database file exists at the specified path.");
            throw;
        }
    }
    
    public string GetCountry(string ip)
    {
        try
        {
            var result = _resolver.Resolve(ip);
            return result is not null ? result.Country : "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get country for IP: {IP}", ip);
            return "Unknown";
        }
    }
}
