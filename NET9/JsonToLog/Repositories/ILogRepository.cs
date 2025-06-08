using JsonToLog.Models;

namespace JsonToLog.Repositories;

public interface ILogRepository
{
    public Task<bool> SendLogAsync(LogSendTask task);
}
