namespace Demo.Application.Services;

public interface IIpToNationService
{
    Task<string> GetNationCodeAsync(string clientIp, CancellationToken ct);
}
