using FluentResults;

namespace Demo.Domain.Repositories;

public interface IIpToNationCache
{
    /// <summary>
    /// Gets the country code for the specified client IP from cache
    /// </summary>
    /// <param name="clientIp"></param>
    /// <returns></returns>
    Task<Result<string>> GetAsync(string clientIp);

    /// <summary>
    /// Sets the country code for the specified client IP in cache
    /// </summary>
    /// <param name="clientIp"></param>
    /// <param name="countryCode"></param>
    /// <param name="ts"></param>
    /// <returns></returns>
    Task SetAsync(string clientIp, string countryCode, TimeSpan ts);
}