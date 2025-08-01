namespace GamePulse.Services.IpToNation;

public interface IIpToNationService
{
    Task<string> GetNationCodeAsync(string clientIp, CancellationToken ct);
}
