namespace Demo.Domain.Repositories;

public interface IIpToNationRepository
{
    Task<string> GetAsync(string clientIp);
}