namespace WebApiApplication.Interfaces;

public interface ISessionRepository
{
    Task<string> GetAsync(string key);
    Task SetAsync(string key, string sessionId);
}
