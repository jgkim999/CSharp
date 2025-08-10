namespace Demo.Application.Repositories;

public interface IIpToNationRepository
{
    Task<string> GetAsync(string clientIp);
}
