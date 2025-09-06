using LiteBus.Events.Abstractions;
using Microsoft.Extensions.Logging;

namespace Demo.Application.ErrorHandlers;

public class EventErrorHandler : IEventErrorHandler
{
    private readonly ILogger<EventErrorHandler> _logger;

    public EventErrorHandler(ILogger<EventErrorHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleErrorAsync(IEvent @event, object? result, Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, "Event 처리 중 오류 발생: {EventType}", @event.GetType().Name);
        await Task.CompletedTask;
    }
}