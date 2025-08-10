using FluentResults;

namespace Demo.Application.Repositories;

/// <summary>
///
/// </summary>
public interface IIpToNationCache
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="clientIp"></param>
    /// <returns></returns>
    Task<Result<string>> GetAsync(string clientIp);

    /// <summary>
    ///
    /// </summary>
    /// <param name="clientIp"></param>
    /// <param name="countryCode"></param>
    /// <param name="ts"></param>
    /// <returns></returns>
    Task SetAsync(string clientIp, string countryCode, TimeSpan ts);
}
