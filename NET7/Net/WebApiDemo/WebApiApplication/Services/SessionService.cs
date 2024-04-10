using Microsoft.Extensions.Logging;
using WebApiApplication.Exceptions;
using WebApiApplication.Interfaces;
using WebApiDomain.Utils;

namespace WebApiApplication.Services;

public class SessionService : ISessionService
{
    private readonly string _prefix = "session:";
    private readonly ISessionRepository _sessionRepository;
    private readonly ILogger<SessionService> _logger;

    public SessionService(ILogger<SessionService> logger, ISessionRepository sessionRepository)
    {
        _logger = logger;
        _sessionRepository = sessionRepository;
    }

    public async Task SetAsync(long userId, string sessionId)
    {
        await _sessionRepository.SetAsync($"{_prefix}{userId}", sessionId);
    }

    public async Task<(bool success, long userId)> IsValid(string sessionId)
    {
        try
        {
            long userId = SessionIdGenerator.GetId(sessionId);
            string savedSessionId = await _sessionRepository.GetAsync($"{_prefix}{userId}");
            return (savedSessionId == sessionId, userId);
        }
        catch (Exception ex)
        {
            throw new GameException(1, "session.IsValid", ex);
        }
    }
}
