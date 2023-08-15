namespace WebApiApplication.Interfaces;

public interface ISessionService
{
    Task SetAsync(long id, string sessionId);
    Task<bool> IsValid(string sessionId);
}
