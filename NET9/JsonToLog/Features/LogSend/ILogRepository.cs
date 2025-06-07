namespace JsonToLog.Features.LogSend;

public interface ILogRepository
{
    public Task<bool> SendLogAsync(LogSendTask task);
}
