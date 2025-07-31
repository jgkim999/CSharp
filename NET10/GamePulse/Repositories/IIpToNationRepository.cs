namespace GamePulse.Repositories;

public interface IIpToNationRepository
{
    Task<string> GetAsync(string clientIp);
}
